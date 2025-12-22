namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    internal static class PasswordRecoveryFaults
    {
        internal const string FAULT_CODE_REQUEST_NULL = "USER_REQUEST_NULL";
        internal const string FAULT_MESSAGE_REQUEST_NULL = "Request cannot be null.";

        internal const string FAULT_CODE_ACCOUNT_NOT_FOUND = "ACCOUNT_NOT_FOUND";
        internal const string FAULT_MESSAGE_ACCOUNT_NOT_FOUND =
            "We could not find an account with the provided information.";

        internal const string FAULT_CODE_RESEND_TOO_FREQUENT = "EMAIL_VERIFICATION_RESEND_TOO_FREQUENT";
        internal const string FAULT_MESSAGE_RESEND_TOO_FREQUENT =
            "You requested a code recently. Please wait a moment and try again.";

        internal const string FAULT_CODE_HOURLY_LIMIT_EXCEEDED = 
            "EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED";
        internal const string FAULT_MESSAGE_HOURLY_LIMIT_EXCEEDED =
            "You have reached the hourly limit for resending verification codes. Please try again later.";

        internal const string FAULT_CODE_VERIFICATION_CODE_INVALID_OR_EXPIRED =
            "EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED";

        internal const string FAULT_CODE_UNEXPECTED_ERROR = "USER_UNEXPECTED_ERROR";
        internal const string FAULT_MESSAGE_UPDATE_PASSWORD_DB_FAILED = 
            "Could not update password in database.";

        internal const string FAULT_MESSAGE_VERIFICATION_CODE_EXPIRED = 
            "The verification code has expired or does not exist.";

        internal const string FAULT_MESSAGE_VERIFICATION_CODE_INVALID = "Invalid verification code.";

        internal const string RECOVERY_AMBIGUOUS_MESSAGE =
            "If the email is registered, a recovery code has been sent.";

        internal const string PASSWORD_RECOVERY_SUCCESS_MESSAGE =
            "A password recovery verification code has been sent to your email.";

    }
}
