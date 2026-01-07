namespace GuessWhoCore.Contracts.Faults
{
    public static class InfrastructureEmailFaultKeys
    {
        public const string CODE_EMAIL_RECIPIENT_INVALID = "INFRA_EMAIL_RECIPIENT_INVALID";
        public const string CODE_EMAIL_CONFIGURATION_MISSING = "INFRA_EMAIL_CONFIGURATION_MISSING";
        public const string CODE_EMAIL_CONFIGURATION_ERROR = "INFRA_EMAIL_CONFIGURATION_ERROR";
        public const string CODE_EMAIL_AUTHENTICATION_FAILED = "INFRA_EMAIL_AUTHENTICATION_FAILED";
        public const string CODE_EMAIL_TIMEOUT = "INFRA_EMAIL_TIMEOUT";
        public const string CODE_EMAIL_UNAVAILABLE = "INFRA_EMAIL_UNAVAILABLE";
        public const string CODE_EMAIL_SEND_FAILED = "INFRA_EMAIL_SEND_FAILED";
        public const string CODE_EMAIL_UNEXPECTED_ERROR = "INFRA_EMAIL_UNEXPECTED_ERROR";
    }
}
