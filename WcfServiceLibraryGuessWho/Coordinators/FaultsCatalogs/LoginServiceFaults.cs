namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    internal static class LoginServiceFaults
    {
        internal const string FAULT_CODE_LOGIN_INVALID_CREDENTIALS = "LOGIN_INVALID_CREDENTIALS";
        internal const string FAULT_CODE_LOGIN_ACCOUNT_LOCKED = "LOGIN_ACCOUNT_LOCKED";
        internal const string FAULT_CODE_LOGIN_PROFILE_ALREADY_ACTIVE = "LOGIN_PROFILE_ACTIVE";
        internal const string FAULT_CODE_LOGIN_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";
        internal const string FAULT_CODE_LOGOUT_FAILED = "LOGOUT_FAILED";
        internal const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DB_TIMEOUT";
        internal const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DB_CONNECTION_LOST";

        internal const string FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR = "An unexpected error occurred. Please try again.";
        internal const string FAULT_MESSAGE_LOGOUT_FAILED = "Could not close session correctly.";
        internal const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT = "Server timeout.";
        internal const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE = "Cannot connect to database.";

        internal const string ERROR_MESSAGE_ARGS_REQUIRED = "Login arguments are required.";
        internal const string ERROR_MESSAGE_EMAIL_REQUIRED = "Email is required.";
        internal const string ERROR_MESSAGE_PASSWORD_REQUIRED = "Password is required.";
        internal const string ERROR_MESSAGE_ACCOUNT_NOT_FOUND = "Account not found.";
        internal const string ERROR_MESSAGE_ACCOUNT_LOCKED = "Account is locked.";
        internal const string ERROR_MESSAGE_PROFILE_ALREADY_ACTIVE = "Profile already active.";
        internal const string ERROR_MESSAGE_INVALID_PASSWORD = "Invalid password.";
        internal const string ERROR_MESSAGE_UPDATE_LOGIN_FAILED = "Failed to update last login.";
    }
}