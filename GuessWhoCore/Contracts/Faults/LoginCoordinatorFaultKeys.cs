namespace GuessWhoCore.Contracts.Faults
{
    public static class LoginCoordinatorFaultKeys
    {
        public const string CODE_USER_ID_INVALID = "LOGINCOORDINATOR_USER_ID_INVALID";
        public const string MSG_USER_ID_INVALID = "LoginCoordinator.UserIdInvalid";
        public const string FALLBACK_USER_ID_INVALID = "The user identifier is not valid.";

        public const string CODE_PROFILE_MARK_ACTIVE_FAILED = "LOGINCOORDINATOR_PROFILE_MARK_ACTIVE_FAILED";
        public const string MSG_PROFILE_MARK_ACTIVE_FAILED = "LoginCoordinator.Profile.MarkActiveFailed";
        public const string FALLBACK_PROFILE_MARK_ACTIVE_FAILED =
            "We could not activate your profile. Please try logging in again.";

        public const string CODE_LOGOUT_TERMINATE_SESSIONS_FAILED = "LOGINCOORDINATOR_LOGOUT_TERMINATE_SESSIONS_FAILED";
        public const string MSG_LOGOUT_TERMINATE_SESSIONS_FAILED = "LoginCoordinator.Logout.TerminateSessionsFailed";
        public const string FALLBACK_LOGOUT_TERMINATE_SESSIONS_FAILED =
            "We could not close your active sessions. Please try again.";

        public const string CODE_LOGOUT_MARK_INACTIVE_FAILED = "LOGINCOORDINATOR_LOGOUT_MARK_INACTIVE_FAILED";
        public const string MSG_LOGOUT_MARK_INACTIVE_FAILED = "LoginCoordinator.Logout.MarkInactiveFailed";
        public const string FALLBACK_LOGOUT_MARK_INACTIVE_FAILED =
            "We could not close your session properly. Please try again.";
    }
}
