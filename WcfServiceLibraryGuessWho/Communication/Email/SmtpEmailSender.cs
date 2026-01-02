using GuessWhoServices.Services.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Communication.Email.Helpers;

namespace GuessWhoServices.Communication.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings smtpSettings; 

        public SmtpEmailSender(SmtpSettings smtpSettings)
        {
            this.smtpSettings = smtpSettings ??
                throw new ArgumentNullException(nameof(smtpSettings));
            
            smtpSettings.Validate();
        }

        public EmailSendResult Send(EmailMessage message)
        {
            if (message == null)
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.UNKNOWN,
                    EmailSafeMessages.SMTP_UNKNOWN);
            }

            if (!EmailValidation.IsValidEmail(message.Recipient))
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.RECIPIENT_INVALID,
                    EmailSafeMessages.RECIPIENT_INVALID);
            }

            if (!smtpSettings.TryValidate(out _))
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.CONFIG_MISSING,
                    EmailSafeMessages.SMTP_CONFIG_MISSING);
            }

            try
            {
                using (var mailMessage = CreateMailMessage(message))
                using (var smtpClient = CreateClient())
                {
                    smtpClient.Send(mailMessage);
                }

                return new EmailSendResult();
            }
            catch (SmtpException ex)
            {
                return SmtpExceptionMapper.Map(ex);
            }
            catch (Exception ex)
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.UNKNOWN,
                    EmailSafeMessages.SMTP_UNKNOWN,
                    ex);
            }
        }

        private MailMessage CreateMailMessage(EmailMessage message)
        {
            return new MailMessage
            {
                From = new MailAddress(smtpSettings.FromAddress, smtpSettings.DisplayName, Encoding.UTF8),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsBodyHtml,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                To = { message.Recipient }
            };
        }

        private SmtpClient CreateClient()
        {
            return new SmtpClient(smtpSettings.Host, smtpSettings.Port)
            {
                EnableSsl = smtpSettings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpSettings.User, smtpSettings.Password),
                Timeout = smtpSettings.TimeoutMs
            };
        }
    }
}
