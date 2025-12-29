namespace GuessWhoCore.Contracts.Faults
{
    public static class UserRegistrationFaultKeys
    {
        public const string CODE_REQUEST_NULL = "USERREGISTRATION_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "UserRegistration.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "Registration data is required.";

        public const string CODE_UNEXPECTED_ERROR = "USERREGISTRATION_UNEXPECTED_ERROR";
        public const string MSG_UNEXPECTED_ERROR = "UserRegistration.UnexpectedError";
        public const string FALLBACK_UNEXPECTED_ERROR =
            "An unexpected error occurred while registering the user. Please try again.";

        public const string CODE_ARGS_REQUIRED = "USERREGISTRATION_ARGS_REQUIRED";
        public const string MSG_ARGS_REQUIRED = "UserRegistration.ArgsRequired";
        public const string FALLBACK_ARGS_REQUIRED = "Registration data is required.";

        public const string CODE_EMAIL_REQUIRED = "USERREGISTRATION_EMAIL_REQUIRED";
        public const string MSG_EMAIL_REQUIRED = "UserRegistration.EmailRequired";
        public const string FALLBACK_EMAIL_REQUIRED = "Email must not be empty.";

        public const string CODE_EMAIL_ALREADY_EXISTS = "USERREGISTRATION_EMAIL_ALREADY_EXISTS";
        public const string MSG_EMAIL_ALREADY_EXISTS = "UserRegistration.EmailAlreadyExists";
        public const string FALLBACK_EMAIL_ALREADY_EXISTS = "A user with this email already exists.";

        public const string CODE_PASSWORD_REQUIRED = "USERREGISTRATION_PASSWORD_REQUIRED";
        public const string MSG_PASSWORD_REQUIRED = "UserRegistration.PasswordRequired";
        public const string FALLBACK_PASSWORD_REQUIRED = "Password must not be empty.";

        public const string CODE_DISPLAYNAME_REQUIRED = "USERREGISTRATION_DISPLAYNAME_REQUIRED";
        public const string MSG_DISPLAYNAME_REQUIRED = "UserRegistration.DisplayNameRequired";
        public const string FALLBACK_DISPLAYNAME_REQUIRED = "Display name must not be empty.";

        public const string CODE_TOKEN_CREATION_FAILED = "USERREGISTRATION_TOKEN_CREATION_FAILED";
        public const string MSG_TOKEN_CREATION_FAILED = "UserRegistration.TokenCreationFailed";
        public const string FALLBACK_TOKEN_CREATION_FAILED = "Could not create email verification token.";

        public const string CODE_NOWUTC_REQUIRED = "USERREGISTRATION_NOWUTC_REQUIRED";
        public const string MSG_NOWUTC_REQUIRED = "UserRegistration.NowUtcRequired";
        public const string FALLBACK_NOWUTC_REQUIRED = "Registration timestamp is required.";
    }
}
