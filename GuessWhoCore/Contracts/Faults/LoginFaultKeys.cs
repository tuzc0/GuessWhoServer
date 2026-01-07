namespace GuessWhoCore.Contracts.Faults
{
    public static class LoginFaultKeys
    {
        public const string CODE_REQUEST_NULL = "LOGIN_REQUEST_NULL";
        public const string CODE_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";
        public const string CODE_INVALID_CREDENTIALS = "LOGIN_INVALID_CREDENTIALS";
        public const string CODE_ACCOUNT_LOCKED = "LOGIN_ACCOUNT_LOCKED";
        public const string CODE_PROFILE_ALREADY_ACTIVE = "LOGIN_PROFILE_ACTIVE";
        public const string CODE_LOGOUT_FAILED = "LOGOUT_FAILED";
    }
}
