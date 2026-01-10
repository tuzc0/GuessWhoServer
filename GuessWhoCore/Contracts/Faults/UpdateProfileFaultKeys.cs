namespace GuessWhoCore.Contracts.Faults
{
    public static class UpdateProfileFaultKeys
    {
        public const string CODE_REQUEST_NULL = "PROFILE_REQUEST_NULL";
        public const string CODE_USER_ID_INVALID = "PROFILE_USER_ID_INVALID";
        public const string CODE_PROFILE_NOT_FOUND = "PROFILE_NOT_FOUND";
        public const string CODE_NO_CHANGES_PROVIDED = "PROFILE_NO_CHANGES_PROVIDED";
        public const string CODE_CURRENT_PASSWORD_REQUIRED = "PROFILE_CURRENT_PASSWORD_REQUIRED";
        public const string CODE_CURRENT_PASSWORD_INCORRECT = "PROFILE_CURRENT_PASSWORD_INCORRECT";
        public const string CODE_UPDATE_FAILED = "PROFILE_UPDATE_FAILED";
        public const string CODE_PROFILE_DELETE_FAILED = "PROFILE_DELETE_FAILED";
        public const string CODE_DISPLAYNAME_INVALID = "PROFILE_DISPLAYNAME_INVALID";
        public const string CODE_AVATAR_INVALID = "PROFILE_AVATAR_INVALID";
        public const string CODE_PASSWORD_INVALID = "PROFILE_PASSWORD_INVALID";
        public const string CODE_EMAIL_NOT_VERIFIED = "UpdateProfile.EmailNotVerified";
    }
}
