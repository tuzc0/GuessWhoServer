using System;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using WcfServiceLibraryGuessWho.Communication.Email.Helpers;

namespace WcfServiceLibraryGuessWho.Communication.Email
{
    internal static class SmtpExceptionMapper
    {
        private const int SOCKET_TIMED_OUT = 10060;

        private static class SmtpAuthTokens
        {
            public const string AUTH = "auth";
            public const string LOGIN = "login";
            public const string CODE_535 = "535";
        }

        internal static EmailSendResult Map(SmtpException ex)
        {
            if (ex == null)
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.UNKNOWN,
                    EmailSafeMessages.SMTP_ERROR);
            }

            if (IsTimeout(ex))
            {
                return new EmailSendResult(
                    EmailSendStatus.TemporaryFailure,
                    EmailInternalCodes.TIMEOUT,
                    EmailSafeMessages.SMTP_TIMEOUT,
                    ex);
            }

            if (IsAuthError(ex))
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.AUTH_ERROR,
                    EmailSafeMessages.SMTP_AUTH_FAILED,
                    ex);
            }

            if (ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst ||
                ex.StatusCode == SmtpStatusCode.CommandNotImplemented)
            {
                return new EmailSendResult(
                    EmailSendStatus.PermanentFailure,
                    EmailInternalCodes.CONFIG_ERROR,
                    EmailSafeMessages.SMTP_CONFIG_ERROR,
                    ex);
            }

            if (ex.StatusCode == SmtpStatusCode.GeneralFailure ||
                ex.StatusCode == SmtpStatusCode.TransactionFailed ||
                ex.StatusCode == SmtpStatusCode.MailboxBusy ||
                ex.StatusCode == SmtpStatusCode.InsufficientStorage)
            {
                return new EmailSendResult(
                    EmailSendStatus.TemporaryFailure,
                    EmailInternalCodes.UNAVAILABLE,
                    EmailSafeMessages.SMTP_UNAVAILABLE,
                    ex);
            }

            return new EmailSendResult(
                EmailSendStatus.PermanentFailure,
                EmailInternalCodes.UNKNOWN,
                EmailSafeMessages.SMTP_UNKNOWN,
                ex);
        }

        private static bool IsTimeout(SmtpException ex)
        {
            if (ex.InnerException is TimeoutException)
            {
                return true;
            }

            if (ex.InnerException is WebException webEx && webEx.Status == WebExceptionStatus.Timeout)
            {
                return true;
            }

            if (ex.InnerException is SocketException socketEx && socketEx.ErrorCode == SOCKET_TIMED_OUT)
            {
                return true;
            }

            return false;
        }

        private static bool IsAuthError(SmtpException ex)
        {
            string message = (ex.Message ?? string.Empty).ToLowerInvariant();

            return message.Contains(SmtpAuthTokens.AUTH) ||
                   message.Contains(SmtpAuthTokens.LOGIN) ||
                   message.Contains(SmtpAuthTokens.CODE_535) ||
                   ex.StatusCode == SmtpStatusCode.ClientNotPermitted;
        }
    }
}
