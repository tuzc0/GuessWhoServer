namespace WcfServiceLibraryGuessWho.Communication.Email.Helpers
{
    internal static class EmailSafeMessages
    {
        public const string RECIPIENT_INVALID = "Invalid recipient email address.";
        public const string SMTP_CONFIG_MISSING =
            "The email service is not configured. Please try again later.";
        public const string SMTP_CONFIG_ERROR = "Email service configuration error.";
        public const string CODE_INVALID = "Invalid verification code format.";
        public const string SMTP_AUTH_FAILED = "Email service authentication failed.";
        public const string SMTP_TIMEOUT =
            "The email server took too long to respond. Please try again.";
        public const string SMTP_UNAVAILABLE = "Email service temporarily unavailable.";
        public const string SMTP_UNKNOWN = "Email service failed to send the email.";
        public const string SMTP_ERROR = "Email service error.";
    }
}
