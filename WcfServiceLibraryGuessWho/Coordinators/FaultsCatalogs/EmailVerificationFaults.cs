namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    internal static class EmailVerificationFaults
    {
        internal const string FAULT_CODE_INVALID_REQUEST = "USER_REQUEST_NULL";
        internal const string FAULT_MESSAGE_INVALID_REQUEST = "Request cannot be null.";

        internal const string FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED = 
            "EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED";
        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED =
            "The verification code is invalid or has expired. Please request a new code.";

        internal const string FAULT_CODE_ACCOUNT_NOT_FOUND = "ACCOUNT_NOT_FOUND";
        internal const string FAULT_MESSAGE_ACCOUNT_NOT_FOUND =
            "We could not find an account with the provided information.";

        internal const string FAULT_CODE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT = 
            "EMAIL_VERIFICATION_RESEND_TOO_FREQUENT";
        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT =
            "You requested a code recently. Please wait a moment and try again.";

        internal const string FAULT_CODE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED = 
            "EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED";
        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED =
            "You have reached the hourly limit for resending verification codes. Please try again later.";

        internal const string FAULT_CODE_EMAIL_VERIFICATION_FAILED = 
            "EMAIL_VERIFICATION_FAILED";
        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_FAILED =
            "No fue posible confirmar el correo electrónico. La cuenta no existe o ya no está disponible.";

        internal const string FAULT_CODE_EMAIL_VERIFICATION_TOKEN_CREATION_FAILED = 
            "EMAIL_VERIFICATION_TOKEN_CREATION_FAILED";
        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_TOKEN_CREATION_FAILED =
            "We could not create the verification code entry. Please try again.";

        internal const string FAULT_CODE_EMAIL_RECIPIENT_INVALID = 
            "EMAIL_RECIPIENT_INVALID";
        internal const string FAULT_MESSAGE_EMAIL_RECIPIENT_INVALID =
            "The destination email address is not valid. Check it and try again.";

        internal const string FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT = 
            "EMAIL_VERIFICATION_CODE_INVALID_FORMAT";
        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT =
            "The verification code must contain exactly 6 digits.";

        internal const string FAULT_CODE_EMAIL_SMTP_CONFIGURATION_MISSING = 
            "EMAIL_SMTP_CONFIGURATION_MISSING";
        internal const string FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_MISSING =
            "The email service is not correctly configured. Please try again later.";

        internal const string FAULT_CODE_EMAIL_SMTP_AUTHENTICATION_FAILED = 
            "EMAIL_SMTP_AUTHENTICATION_FAILED";
        internal const string FAULT_MESSAGE_EMAIL_SMTP_AUTHENTICATION_FAILED =
            "The email service could not authenticate with the server. Please try again later.";

        internal const string FAULT_CODE_EMAIL_SMTP_CONFIGURATION_ERROR = 
            "EMAIL_SMTP_CONFIGURATION_ERROR";
        internal const string FAULT_MESSAGE_EMAIL_SMTP_CONFIGURATION_ERROR =
            "The email service is not available due to a configuration problem. Please try again later.";

        internal const string FAULT_CODE_EMAIL_SMTP_UNAVAILABLE = "EMAIL_SMTP_UNAVAILABLE";
        internal const string FAULT_MESSAGE_EMAIL_SMTP_UNAVAILABLE =
            "The email service is temporarily unavailable. Please try again later.";

        internal const string FAULT_CODE_EMAIL_SEND_FAILED = "EMAIL_SEND_FAILED";
        internal const string FAULT_MESSAGE_EMAIL_SEND_FAILED =
            "We could not send the verification email. Please try again later.";

        internal const string FAULT_CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE = 
            "CRYPTO_RANDOM_GENERATOR_UNAVAILABLE";
        internal const string FAULT_MESSAGE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE =
            "The system could not generate a secure verification code. Please try again later.";

        internal const string FAULT_CODE_VERIFICATION_CODE_GENERATION_FAILED = 
            "VERIFICATION_CODE_GENERATION_FAILED";
        internal const string FAULT_MESSAGE_VERIFICATION_CODE_GENERATION_FAILED =
            "We could not generate a verification code. Please try again.";

        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_EXPIRED_OR_MISSING =
            "The verification code has expired or is no longer valid. Please request a new code.";

        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INCORRECT =
            "The verification code is incorrect. Please check the code and try again.";

        internal const string FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_ALREADY_USED =
            "This verification code was already used or is no longer valid. Please request a new code.";
    }
}
