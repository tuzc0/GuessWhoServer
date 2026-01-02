using System;

namespace GuessWhoServices.Communication.Email
{
    public enum EmailSendStatus
    {
        Success = 0,
        TemporaryFailure = 1,
        PermanentFailure = 2
    }

    public class EmailSendResult
    {
        public EmailSendStatus Status { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }

        public Exception TechnicalException { get; }

        public bool IsSuccess => Status == EmailSendStatus.Success;

        public EmailSendResult()
        {
            Status = EmailSendStatus.Success;
            ErrorCode = string.Empty;
            Message = string.Empty;
            TechnicalException = null;
        }

        public EmailSendResult(EmailSendStatus status, string errorCode, string message)
        {
            Status = status;
            ErrorCode = errorCode ?? string.Empty;
            Message = message ?? string.Empty;
            TechnicalException= null;
        }

        public EmailSendResult(EmailSendStatus status, string errorCode, string message, Exception ex) : this(status, errorCode, message) 
        {
            TechnicalException = ex; 
        }
    }
}
