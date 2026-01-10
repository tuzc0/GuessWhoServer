namespace GuessWhoCore.Validation
{
    public static class UserValidationCodes
    {
        public const string INVALID_REQUEST = "USER_INVALID_REQUEST";

        public const string EMAIL_REQUIRED = "USER_EMAIL_REQUIRED";
        public const string EMAIL_TOO_LONG = "USER_EMAIL_TOO_LONG";
        public const string EMAIL_INVALID_FORMAT = "USER_EMAIL_INVALID_FORMAT";

        public const string DISPLAY_NAME_REQUIRED = "USER_DISPLAY_NAME_REQUIRED";
        public const string DISPLAY_NAME_TOO_SHORT = "USER_DISPLAY_NAME_TOO_SHORT";
        public const string DISPLAY_NAME_TOO_LONG = "USER_DISPLAY_NAME_TOO_LONG";
        public const string DISPLAY_NAME_INVALID_FORMAT = "USER_DISPLAY_NAME_INVALID_FORMAT";

        public const string PASSWORD_REQUIRED = "USER_PASSWORD_REQUIRED";
        public const string PASSWORD_INVALID_FORMAT = "PASSWORD_INVALID_FORMAT";
        public const string PASSWORD_TOO_SHORT = "USER_PASSWORD_TOO_SHORT";
        public const string PASSWORD_TOO_LONG = "USER_PASSWORD_TOO_LONG";

        public const string CONFIRM_PASSWORD_REQUIRED = "USER_CONFIRM_PASSWORD_REQUIRED";
        public const string CONFIRM_PASSWORD_MISMATCH = "USER_CONFIRM_PASSWORD_MISMATCH";

        public const string AVATAR_ID_TOO_LONG = "USER_AVATAR_ID_TOO_LONG";

        public const string CURRENT_PASSWORD_REQUIRED = "USER_CURRENT_PASSWORD_REQUIRED";
    }
}
