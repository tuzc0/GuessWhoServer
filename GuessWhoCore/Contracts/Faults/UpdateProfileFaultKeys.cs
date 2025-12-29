namespace GuessWhoCore.Contracts.Faults
{
    public static class UpdateProfileFaultKeys
    {
        public const string CODE_REQUEST_NULL = "PROFILE_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "Profile.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "Profile request data is missing. Please try again.";

        public const string CODE_USER_ID_INVALID = "PROFILE_USER_ID_INVALID";
        public const string MSG_USER_ID_INVALID = "Profile.UserIdInvalid";
        public const string FALLBACK_USER_ID_INVALID = "The profile identifier is not valid.";

        public const string CODE_PROFILE_NOT_FOUND = "PROFILE_NOT_FOUND";
        public const string MSG_PROFILE_NOT_FOUND = "Profile.NotFound";
        public const string FALLBACK_PROFILE_NOT_FOUND = "We could not find a profile for the specified user.";

        public const string CODE_NO_CHANGES_PROVIDED = "PROFILE_NO_CHANGES_PROVIDED";
        public const string MSG_NO_CHANGES_PROVIDED = "Profile.NoChangesProvided";
        public const string FALLBACK_NO_CHANGES_PROVIDED = "No profile changes were provided.";

        public const string CODE_CURRENT_PASSWORD_REQUIRED = "PROFILE_CURRENT_PASSWORD_REQUIRED";
        public const string MSG_CURRENT_PASSWORD_REQUIRED = "Profile.CurrentPasswordRequired";
        public const string FALLBACK_CURRENT_PASSWORD_REQUIRED = "The current password is required to change your password.";

        public const string CODE_CURRENT_PASSWORD_INCORRECT = "PROFILE_CURRENT_PASSWORD_INCORRECT";
        public const string MSG_CURRENT_PASSWORD_INCORRECT = "Profile.CurrentPasswordIncorrect";
        public const string FALLBACK_CURRENT_PASSWORD_INCORRECT = "The current password you entered is incorrect.";

        public const string CODE_UPDATE_FAILED = "PROFILE_UPDATE_FAILED";
        public const string MSG_UPDATE_FAILED = "Profile.UpdateFailed";
        public const string FALLBACK_UPDATE_FAILED = "An unexpected error occurred while updating your profile. Please try again later.";

        public const string CODE_PROFILE_DELETE_FAILED = "PROFILE_DELETE_FAILED";
        public const string MSG_PROFILE_DELETE_FAILED = "Profile.DeleteFailed";
        public const string FALLBACK_PROFILE_DELETE_FAILED = "We could not delete your profile. Please try again later.";
    }
}
