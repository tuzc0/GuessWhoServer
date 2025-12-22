namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    public static class LoginServiceFaults
    {
        public const string FAULT_CODE_LOGIN_INVALID_CREDENTIALS = "LOGIN_INVALID_CREDENTIALS";
        public const string FAULT_CODE_LOGIN_ACCOUNT_LOCKED = "LOGIN_ACCOUNT_LOCKED";
        public const string FAULT_CODE_LOGIN_PROFILE_ALREADY_ACTIVE = "LOGIN_PROFILE_ACTIVE";
        public const string FAULT_CODE_LOGIN_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";
        public const string FAULT_CODE_LOGOUT_FAILED = "LOGOUT_FAILED";
        public const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DB_TIMEOUT";
        public const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DB_CONNECTION_LOST";

        public const string FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR = "An unexpected error occurred. Please try again.";
        public const string FAULT_MESSAGE_LOGOUT_FAILED = "Could not close session correctly.";
        public const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT = "Server timeout.";
        public const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE = "Cannot connect to database.";

        public const string ERROR_MESSAGE_ARGS_REQUIRED = "Login arguments are required.";
        public const string ERROR_MESSAGE_EMAIL_REQUIRED = "Email is required.";
        public const string ERROR_MESSAGE_PASSWORD_REQUIRED = "Password is required.";
        public const string ERROR_MESSAGE_ACCOUNT_NOT_FOUND = "Account not found.";
        public const string ERROR_MESSAGE_ACCOUNT_LOCKED = "Account is locked.";
        public const string ERROR_MESSAGE_PROFILE_ALREADY_ACTIVE = "Profile already active.";
        public const string ERROR_MESSAGE_INVALID_PASSWORD = "Invalid password.";
        public const string ERROR_MESSAGE_UPDATE_LOGIN_FAILED = "Failed to update last login.";
    }
}