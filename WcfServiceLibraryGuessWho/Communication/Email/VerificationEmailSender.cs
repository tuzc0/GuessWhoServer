using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace WcfServiceLibraryGuessWho.Communication.Email
{
    public sealed class VerificationEmailSender : IVerificationEmailSender
    {
        private readonly string smtpHost;
        private readonly int smtpPort;
        private readonly bool secureSslConnection;
        private readonly string smtpUser;
        private readonly string smtpPassword;
        private readonly string fromAddress;
        private readonly string displayName;
        private readonly int timeoutMs;

        public VerificationEmailSender()
        {
            var app = ConfigurationManager.AppSettings;

            smtpHost = app["Smtp.Host"] ?? "smtp.gmail.com";
            smtpPort = int.TryParse(app["Smtp.Port"], out var parsedPort) ? parsedPort : 587;
            secureSslConnection = !bool.TryParse(app["Smtp.EnableSsl"], out var enableSsl) || enableSsl;
            smtpUser = app["Smtp.User"];
            smtpPassword = app["Smtp.Password"];
            fromAddress = app["Smtp.From"] ?? smtpUser;
            displayName = app["Smtp.DisplayName"] ?? "GuessWho";
            timeoutMs = int.TryParse(app["Smtp.TimeoutMs"], out var parsedTimeout) ? parsedTimeout : 15000;

            if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
            {
                throw new EmailSendException("SMTPNotConfigured", "SMTP settings are missing");
            }
        }
        public void SendVerificationCode(string recipientEmailAddress, string verificationCode)
        {
            if (!IsValidEmail(recipientEmailAddress))
            {
                throw new EmailSendException("EmailInvalid", "Invalid recipient email.");
            }

            if (!Regex.IsMatch(verificationCode ?? string.Empty, @"^\d{6}$"))
            {
                throw new EmailSendException("VerificationCodeInvalid", "Invalid verification code must be 6 digits.");
            }

            string subjectText = SanitizeSingleLine("Your verification code");
            string bodyText = $"Your code is: {verificationCode}\nIt expires in 10 minutes.";

            using (var mailMessage = new MailMessage())
            {

                mailMessage.From = new MailAddress(fromAddress, displayName, Encoding.UTF8);
                mailMessage.To.Add(recipientEmailAddress.Trim());

                mailMessage.Subject = subjectText;
                mailMessage.Body = bodyText;
                mailMessage.IsBodyHtml = false;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.HeadersEncoding = Encoding.UTF8;

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.EnableSsl = secureSslConnection;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                    smtpClient.Timeout = timeoutMs;

                    try
                    {
                        smtpClient.Send(mailMessage);
                    }
                    catch (SmtpException ex) when (
                           ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst ||
                           ex.StatusCode == SmtpStatusCode.ClientNotPermitted ||
                           ex.StatusCode == SmtpStatusCode.CommandNotImplemented)
                    {
                        throw new EmailSendException("SMTPConfigError", "SMTP configuration error.", ex);
                    }
                    catch (SmtpException ex) when (
                           ex.StatusCode == SmtpStatusCode.GeneralFailure ||
                           ex.StatusCode == SmtpStatusCode.TransactionFailed ||
                           ex.StatusCode == SmtpStatusCode.MailboxBusy ||
                           ex.StatusCode == SmtpStatusCode.InsufficientStorage)
                    {
                        throw new EmailSendException("SMTPUnavailable", "Email service unavailable.", ex);
                    }
                    catch (SmtpException ex) when (IsAuthError(ex))
                    {
                        throw new EmailSendException("SMTPAuthenticationFailed", "SMTP authentication failed.", ex);
                    }
                    catch (SmtpException ex)
                    {
                        throw new EmailSendException("EmailSendFailed", "Unable to send verification email.", ex);
                    }
                    catch (Exception ex)
                    {
                        throw new EmailSendException("EmailSendFailed", "Unable to send verification email.", ex);
                    }
                }
            }
        }

        private static bool IsValidEmail(string email)
        {
            try 
            { 
                var address = new MailAddress(email); return address.Address == email; 
            }
            catch 
            { 
                return false; 
            }
        }

        private static string SanitizeSingleLine(string text) =>
            (text ?? string.Empty).Replace("\r", "").Replace("\n", "").Trim();

        private static bool IsAuthError(SmtpException ex)
        {
            var message = (ex.Message ?? string.Empty).ToUpperInvariant();

            if (message.Contains("535") || message.Contains("5.7.8") || message.Contains("5.7.0"))
            {
                return true;
            }

            if (message.Contains("AUTH") || message.Contains("AUTHENTIC") || message.Contains("LOGIN") || message.Contains("CREDENTIAL"))
            {
                return true;
            }

            if (ex.StatusCode == SmtpStatusCode.ClientNotPermitted)
            {
                return true;
            }

            return false;
        }

    }
}
