namespace GuessWhoCore.Contracts.Faults
{
    public static class InfrastructureEmailFaultKeys
    {
        public const string CODE_EMAIL_RECIPIENT_INVALID = "INFRA_EMAIL_RECIPIENT_INVALID";
        public const string MSG_EMAIL_RECIPIENT_INVALID = "Infrastructure.Email.RecipientInvalid";
        public const string FALLBACK_EMAIL_RECIPIENT_INVALID =
            "The destination email address is not valid. Please verify it and try again.";

        public const string CODE_EMAIL_CONFIGURATION_MISSING = "INFRA_EMAIL_CONFIGURATION_MISSING";
        public const string MSG_EMAIL_CONFIGURATION_MISSING = "Infrastructure.Email.ConfigurationMissing";
        public const string FALLBACK_EMAIL_CONFIGURATION_MISSING =
            "The email service is not configured. Please try again later.";

        public const string CODE_EMAIL_CONFIGURATION_ERROR = "INFRA_EMAIL_CONFIGURATION_ERROR";
        public const string MSG_EMAIL_CONFIGURATION_ERROR = "Infrastructure.Email.ConfigurationError";
        public const string FALLBACK_EMAIL_CONFIGURATION_ERROR =
            "The email service configuration is invalid. Please try again later.";

        public const string CODE_EMAIL_AUTHENTICATION_FAILED = "INFRA_EMAIL_AUTHENTICATION_FAILED";
        public const string MSG_EMAIL_AUTHENTICATION_FAILED = "Infrastructure.Email.AuthenticationFailed";
        public const string FALLBACK_EMAIL_AUTHENTICATION_FAILED =
            "The email service could not authenticate with the mail server. Please try again later.";

        public const string CODE_EMAIL_TIMEOUT = "INFRA_EMAIL_TIMEOUT";
        public const string MSG_EMAIL_TIMEOUT = "Infrastructure.Email.Timeout";
        public const string FALLBACK_EMAIL_TIMEOUT =
            "The email server took too long to respond. Please try again.";

        public const string CODE_EMAIL_UNAVAILABLE = "INFRA_EMAIL_UNAVAILABLE";
        public const string MSG_EMAIL_UNAVAILABLE = "Infrastructure.Email.Unavailable";
        public const string FALLBACK_EMAIL_UNAVAILABLE =
            "The email service is temporarily unavailable. Please try again later.";

        public const string CODE_EMAIL_SEND_FAILED = "INFRA_EMAIL_SEND_FAILED";
        public const string MSG_EMAIL_SEND_FAILED = "Infrastructure.Email.SendFailed";
        public const string FALLBACK_EMAIL_SEND_FAILED =
            "We could not send the email. Please try again later.";

        public const string CODE_EMAIL_UNEXPECTED_ERROR = "INFRA_EMAIL_UNEXPECTED_ERROR";
        public const string MSG_EMAIL_UNEXPECTED_ERROR = "Infrastructure.Email.UnexpectedError";
        public const string FALLBACK_EMAIL_UNEXPECTED_ERROR =
            "Unexpected email infrastructure error. Please try again later.";
    }
}
