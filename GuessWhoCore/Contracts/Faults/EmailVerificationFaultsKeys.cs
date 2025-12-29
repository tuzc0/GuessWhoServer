namespace GuessWhoCore.Contracts.Faults
{
    public static class EmailVerificationFaultKeys
    {
        public const string CODE_REQUEST_NULL = "EMAILVERIFICATION_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "EmailVerification.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "Request cannot be null.";

        public const string CODE_UNEXPECTED_ERROR = "EMAILVERIFICATION_UNEXPECTED_ERROR";
        public const string MSG_UNEXPECTED_ERROR = "EmailVerification.UnexpectedError";
        public const string FALLBACK_UNEXPECTED_ERROR =
            "An unexpected error occurred while verifying email. Please try again.";

        public const string CODE_ACCOUNT_NOT_FOUND = "EMAILVERIFICATION_ACCOUNT_NOT_FOUND";
        public const string MSG_ACCOUNT_NOT_FOUND = "EmailVerification.AccountNotFound";
        public const string FALLBACK_ACCOUNT_NOT_FOUND =
            "We could not find an account with the provided information.";

        public const string CODE_CODE_INVALID_OR_EXPIRED = "EMAILVERIFICATION_CODE_INVALID_OR_EXPIRED";
        public const string MSG_CODE_INVALID_OR_EXPIRED = "EmailVerification.CodeInvalidOrExpired";
        public const string FALLBACK_CODE_INVALID_OR_EXPIRED =
            "The verification code is invalid or has expired. Please request a new code.";

        public const string CODE_RESEND_TOO_FREQUENT = "EMAILVERIFICATION_RESEND_TOO_FREQUENT";
        public const string MSG_RESEND_TOO_FREQUENT = "EmailVerification.ResendTooFrequent";
        public const string FALLBACK_RESEND_TOO_FREQUENT =
            "You requested a code recently. Please wait a moment and try again.";

        public const string CODE_RESEND_HOURLY_LIMIT_EXCEEDED = "EMAILVERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED";
        public const string MSG_RESEND_HOURLY_LIMIT_EXCEEDED = "EmailVerification.HourlyLimitExceeded";
        public const string FALLBACK_RESEND_HOURLY_LIMIT_EXCEEDED =
            "You have reached the hourly limit for resending verification codes. Please try again later.";

        public const string CODE_EMAIL_VERIFICATION_FAILED = "EMAILVERIFICATION_VERIFICATION_FAILED";
        public const string MSG_EMAIL_VERIFICATION_FAILED = "EmailVerification.VerificationFailed";
        public const string FALLBACK_EMAIL_VERIFICATION_FAILED =
            "No fue posible confirmar el correo electrónico. La cuenta no existe o ya no está disponible.";

        public const string CODE_TOKEN_CREATION_FAILED = "EMAILVERIFICATION_TOKEN_CREATION_FAILED";
        public const string MSG_TOKEN_CREATION_FAILED = "EmailVerification.TokenCreationFailed";
        public const string FALLBACK_TOKEN_CREATION_FAILED =
            "We could not create the verification code entry. Please try again.";

        public const string CODE_EMAIL_RECIPIENT_INVALID = "EMAILVERIFICATION_RECIPIENT_INVALID";
        public const string MSG_EMAIL_RECIPIENT_INVALID = "EmailVerification.RecipientInvalid";
        public const string FALLBACK_EMAIL_RECIPIENT_INVALID =
            "The destination email address is not valid. Check it and try again.";

        public const string CODE_CODE_INVALID_FORMAT = "EMAILVERIFICATION_CODE_INVALID_FORMAT";
        public const string MSG_CODE_INVALID_FORMAT = "EmailVerification.CodeInvalidFormat";
        public const string FALLBACK_CODE_INVALID_FORMAT =
            "The verification code must contain exactly 6 digits.";

        public const string CODE_SMTP_CONFIGURATION_MISSING = "EMAILVERIFICATION_SMTP_CONFIGURATION_MISSING";
        public const string MSG_SMTP_CONFIGURATION_MISSING = "EmailVerification.SmtpConfigurationMissing";
        public const string FALLBACK_SMTP_CONFIGURATION_MISSING =
            "The email service is not correctly configured. Please try again later.";

        public const string CODE_SMTP_AUTHENTICATION_FAILED = "EMAILVERIFICATION_SMTP_AUTHENTICATION_FAILED";
        public const string MSG_SMTP_AUTHENTICATION_FAILED = "EmailVerification.SmtpAuthenticationFailed";
        public const string FALLBACK_SMTP_AUTHENTICATION_FAILED =
            "The email service could not authenticate with the server. Please try again later.";

        public const string CODE_SMTP_CONFIGURATION_ERROR = "EMAILVERIFICATION_SMTP_CONFIGURATION_ERROR";
        public const string MSG_SMTP_CONFIGURATION_ERROR = "EmailVerification.SmtpConfigurationError";
        public const string FALLBACK_SMTP_CONFIGURATION_ERROR =
            "The email service is not available due to a configuration problem. Please try again later.";

        public const string CODE_SMTP_UNAVAILABLE = "EMAILVERIFICATION_SMTP_UNAVAILABLE";
        public const string MSG_SMTP_UNAVAILABLE = "EmailVerification.SmtpUnavailable";
        public const string FALLBACK_SMTP_UNAVAILABLE =
            "The email service is temporarily unavailable. Please try again later.";
        
        public const string CODE_EMAIL_SEND_FAILED = "EMAILVERIFICATION_EMAIL_SEND_FAILED";
        public const string MSG_EMAIL_SEND_FAILED = "EmailVerification.EmailSendFailed";
        public const string FALLBACK_EMAIL_SEND_FAILED =
            "We could not send the verification email. Please try again later.";

        public const string CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE = "EMAILVERIFICATION_CRYPTO_RANDOM_UNAVAILABLE";
        public const string MSG_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE = "EmailVerification.CryptoRandomUnavailable";
        public const string FALLBACK_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE =
            "The system could not generate a secure verification code. Please try again later.";

        public const string CODE_VERIFICATION_CODE_GENERATION_FAILED = "EMAILVERIFICATION_CODE_GENERATION_FAILED";
        public const string MSG_VERIFICATION_CODE_GENERATION_FAILED = "EmailVerification.CodeGenerationFailed";
        public const string FALLBACK_VERIFICATION_CODE_GENERATION_FAILED =
            "We could not generate a verification code. Please try again.";

        public const string MSG_CODE_EXPIRED_OR_MISSING = "EmailVerification.CodeExpiredOrMissing";
        public const string FALLBACK_CODE_EXPIRED_OR_MISSING =
            "The verification code has expired or is no longer valid. Please request a new code.";

        public const string MSG_CODE_INCORRECT = "EmailVerification.CodeIncorrect";
        public const string FALLBACK_CODE_INCORRECT =
            "The verification code is incorrect. Please check the code and try again.";

        public const string MSG_CODE_ALREADY_USED = "EmailVerification.CodeAlreadyUsed";
        public const string FALLBACK_CODE_ALREADY_USED =
            "This verification code was already used or is no longer valid. Please request a new code.";
    }
}
