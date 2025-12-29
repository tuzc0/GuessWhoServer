namespace GuessWhoCore.Contracts.Faults
{
    public static class PasswordRecoveryFaultKeys
    {
        public const string CODE_REQUEST_NULL = "PASSWORDRECOVERY_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "PasswordRecovery.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "Request cannot be null.";

        public const string CODE_UNEXPECTED_ERROR = "PASSWORDRECOVERY_UNEXPECTED_ERROR";
        public const string MSG_UNEXPECTED_ERROR = "PasswordRecovery.UnexpectedError";
        public const string FALLBACK_UNEXPECTED_ERROR =
            "An unexpected error occurred while recovering the password. Please try again.";

        public const string CODE_ACCOUNT_NOT_FOUND = "PASSWORDRECOVERY_ACCOUNT_NOT_FOUND";
        public const string MSG_ACCOUNT_NOT_FOUND = "PasswordRecovery.AccountNotFound";
        public const string FALLBACK_ACCOUNT_NOT_FOUND =
            "We could not find an account with the provided information.";

        public const string CODE_RESEND_TOO_FREQUENT = "PASSWORDRECOVERY_RESEND_TOO_FREQUENT";
        public const string MSG_RESEND_TOO_FREQUENT = "PasswordRecovery.ResendTooFrequent";
        public const string FALLBACK_RESEND_TOO_FREQUENT =
            "You requested a code recently. Please wait a moment and try again.";

        public const string CODE_RESEND_HOURLY_LIMIT_EXCEEDED = "PASSWORDRECOVERY_RESEND_HOURLY_LIMIT_EXCEEDED";
        public const string MSG_RESEND_HOURLY_LIMIT_EXCEEDED = "PasswordRecovery.HourlyLimitExceeded";
        public const string FALLBACK_RESEND_HOURLY_LIMIT_EXCEEDED =
            "You have reached the hourly limit for resending verification codes. Please try again later.";

        public const string CODE_CODE_INVALID_OR_EXPIRED = "PASSWORDRECOVERY_CODE_INVALID_OR_EXPIRED";
        public const string MSG_CODE_INVALID_OR_EXPIRED = "PasswordRecovery.CodeInvalidOrExpired";
        public const string FALLBACK_CODE_INVALID_OR_EXPIRED =
            "The verification code is invalid or has expired.";

        public const string MSG_CODE_EXPIRED = "PasswordRecovery.CodeExpired";
        public const string FALLBACK_CODE_EXPIRED = "The verification code has expired or does not exist.";

        public const string MSG_CODE_INVALID = "PasswordRecovery.CodeInvalid";
        public const string FALLBACK_CODE_INVALID = "Invalid verification code.";

        public const string CODE_UPDATE_PASSWORD_DB_FAILED = "PASSWORDRECOVERY_UPDATE_PASSWORD_DB_FAILED";
        public const string MSG_UPDATE_PASSWORD_DB_FAILED = "PasswordRecovery.UpdatePasswordDbFailed";
        public const string FALLBACK_UPDATE_PASSWORD_DB_FAILED = "Could not update password in database.";

        public const string CODE_TOKEN_CREATION_FAILED = "PASSWORDRECOVERY_TOKEN_CREATION_FAILED";
        public const string MSG_TOKEN_CREATION_FAILED = "PasswordRecovery.TokenCreationFailed";
        public const string FALLBACK_TOKEN_CREATION_FAILED =
            "We could not create the password recovery verification code. Please try again.";

        public const string MSG_AMBIGUOUS_SUCCESS = "PasswordRecovery.AmbiguousSuccess";
        public const string FALLBACK_AMBIGUOUS_SUCCESS =
            "If the email is registered, a recovery code has been sent.";

        public const string MSG_RECOVERY_SENT = "PasswordRecovery.RecoverySent";
        public const string FALLBACK_RECOVERY_SENT =
            "A password recovery verification code has been sent to your email.";
    }
}
