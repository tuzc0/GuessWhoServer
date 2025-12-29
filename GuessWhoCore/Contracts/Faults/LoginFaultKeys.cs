namespace GuessWhoCore.Contracts.Faults
{
    public static class LoginFaultKeys
    {
        public const string CODE_REQUEST_NULL = "LOGIN_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "Login.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "Login request cannot be null.";

        public const string CODE_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";
        public const string MSG_UNEXPECTED_ERROR = "Login.UnexpectedError";
        public const string FALLBACK_UNEXPECTED_ERROR = "An unexpected error occurred. Please try again.";

        public const string CODE_INVALID_CREDENTIALS = "LOGIN_INVALID_CREDENTIALS";
        public const string MSG_INVALID_CREDENTIALS = "Login.InvalidCredentials";
        public const string FALLBACK_INVALID_CREDENTIALS = "Invalid credentials.";

        public const string CODE_ACCOUNT_LOCKED = "LOGIN_ACCOUNT_LOCKED";
        public const string MSG_ACCOUNT_LOCKED = "Login.AccountLocked";
        public const string FALLBACK_ACCOUNT_LOCKED = "Account is locked.";

        public const string CODE_PROFILE_ALREADY_ACTIVE = "LOGIN_PROFILE_ACTIVE";
        public const string MSG_PROFILE_ALREADY_ACTIVE = "Login.ProfileAlreadyActive";
        public const string FALLBACK_PROFILE_ALREADY_ACTIVE = "Profile already active.";

        public const string CODE_LOGOUT_FAILED = "LOGOUT_FAILED";
        public const string MSG_LOGOUT_FAILED = "Login.LogoutFailed";
        public const string FALLBACK_LOGOUT_FAILED = "Could not close session correctly.";
    }
}
