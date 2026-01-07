namespace GuessWhoCore.Contracts.Faults
{
    public static class LoginCoordinatorFaultKeys
    {
        public const string CODE_USER_ID_INVALID = "LOGINCOORDINATOR_USER_ID_INVALID";
        public const string CODE_PROFILE_MARK_ACTIVE_FAILED = "LOGINCOORDINATOR_PROFILE_MARK_ACTIVE_FAILED";
        public const string CODE_LOGOUT_TERMINATE_SESSIONS_FAILED = "LOGINCOORDINATOR_LOGOUT_TERMINATE_SESSIONS_FAILED";
        public const string CODE_LOGOUT_MARK_INACTIVE_FAILED = "LOGINCOORDINATOR_LOGOUT_MARK_INACTIVE_FAILED";
    }
}
