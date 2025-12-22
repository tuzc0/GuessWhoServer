using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Net.Mail;
using System.Security.Authentication;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;

namespace WcfServiceLibraryGuessWho.Coordinators.EmailVerification
{
    public class VerificationEmailDispatcher : IVerificationEmailDispatcher
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(VerificationEmailDispatcher));

        private const string EMAIL_SENDER_PARAM_RECIPIENT = "recipientEmailAddress";
        private const string EMAIL_SENDER_PARAM_CODE = "verificationCode";

        private readonly IVerificationEmailSender verificationEmailSender;

        public VerificationEmailDispatcher(IVerificationEmailSender verificationEmailSender)
        {
            this.verificationEmailSender = verificationEmailSender ?? 
                throw new ArgumentNullException(nameof(verificationEmailSender));
        }

        public void SendVerificationEmailOrThrow(string recipientEmailAddress, string verificationCode)
        {
            try
            {
                verificationEmailSender.SendVerificationCode(recipientEmailAddress, verificationCode);
            }
            catch (EmailSendException ex)
            {
                Logger.Error("EmailSendException while sending verification email.", ex);

                throw Faults.Create(ex.Code, ex.Message, ex);
            }
            catch (ArgumentException ex) when (ex.ParamName == EMAIL_SENDER_PARAM_RECIPIENT)
            {
                Logger.Warn("Invalid recipient email address while sending verification email.", ex);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_RECIPIENT_INVALID,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_RECIPIENT_INVALID,
                    ex);
            }
            catch (ArgumentException ex) when (ex.ParamName == EMAIL_SENDER_PARAM_CODE)
            {
                Logger.Warn("Invalid verification code format while sending verification email.", ex);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT,
                    ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Fatal("SMTP configuration missing or invalid while sending verification email.", ex);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_SMTP_CONFIGURATION_MISSING,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_MISSING,
                    ex);
            }
            catch (AuthenticationException ex)
            {
                Logger.Fatal("SMTP authentication failed while sending verification email.", ex);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_SMTP_AUTHENTICATION_FAILED,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_SMTP_AUTHENTICATION_FAILED,
                    ex);
            }
            catch (SmtpException ex) when (
                ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst ||
                ex.StatusCode == SmtpStatusCode.ClientNotPermitted ||
                ex.StatusCode == SmtpStatusCode.CommandNotImplemented)
            {
                Logger.Fatal("SMTP configuration error while sending verification email.", ex);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_SMTP_CONFIGURATION_ERROR,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_ERROR,
                    ex);
            }
            catch (SmtpException ex) when (
                ex.StatusCode == SmtpStatusCode.GeneralFailure ||
                ex.StatusCode == SmtpStatusCode.TransactionFailed ||
                ex.StatusCode == SmtpStatusCode.MailboxBusy ||
                ex.StatusCode == SmtpStatusCode.InsufficientStorage)
            {
                Logger.Error("SMTP unavailable while sending verification email.", ex);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_SMTP_UNAVAILABLE,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_SMTP_UNAVAILABLE,
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error while sending verification email.", ex);
                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_SEND_FAILED,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_SEND_FAILED,
                    ex);
            }
        }
    }
}
