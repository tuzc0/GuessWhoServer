namespace GuessWhoCore.Contracts.Faults
{
    public static class InfrastructureFaultKeys
    {
        public const string CODE_UNEXPECTED_ERROR = "INFRA_UNEXPECTED_ERROR";
        public const string MSG_UNEXPECTED_ERROR = "Infrastructure.UnexpectedError";
        public const string FALLBACK_UNEXPECTED_ERROR =
            "Unexpected infrastructure error. Please try again later.";

        public const string CODE_DATABASE_COMMAND_TIMEOUT = "INFRA_DB_COMMAND_TIMEOUT";
        public const string MSG_DATABASE_COMMAND_TIMEOUT = "Infrastructure.Database.CommandTimeout";
        public const string FALLBACK_DATABASE_COMMAND_TIMEOUT =
            "The server took too long to respond. Please try again.";

        public const string CODE_DATABASE_CONNECTION_FAILURE = "INFRA_DB_CONNECTION_FAILURE";
        public const string MSG_DATABASE_CONNECTION_FAILURE = "Infrastructure.Database.ConnectionFailure";
        public const string FALLBACK_DATABASE_CONNECTION_FAILURE =
            "The server could not connect to the database. Please try again later.";

        public const string CODE_DATABASE_NOT_FOUND = "INFRA_DB_NOT_FOUND";
        public const string MSG_DATABASE_NOT_FOUND = "Infrastructure.Database.NotFound";
        public const string FALLBACK_DATABASE_NOT_FOUND = "The database is unavailable or does not exist.";

        public const string CODE_DATABASE_NETWORK_PATH_NOT_FOUND = "INFRA_DB_NETWORK_PATH_NOT_FOUND";
        public const string MSG_DATABASE_NETWORK_PATH_NOT_FOUND = "Infrastructure.Database.NetworkPathNotFound";
        public const string FALLBACK_DATABASE_NETWORK_PATH_NOT_FOUND =
            "The database network path could not be found. Please try again later.";

        public const string CODE_DATABASE_UNREACHABLE = "INFRA_DB_UNREACHABLE";
        public const string MSG_DATABASE_UNREACHABLE = "Infrastructure.Database.Unreachable";
        public const string FALLBACK_DATABASE_UNREACHABLE =
            "The database service is unreachable. Please try again later.";

        public const string CODE_DATABASE_SERVER_NOT_FOUND = "INFRA_DB_SERVER_NOT_FOUND";
        public const string MSG_DATABASE_SERVER_NOT_FOUND = "Infrastructure.Database.ServerNotFound";
        public const string FALLBACK_DATABASE_SERVER_NOT_FOUND =
            "The database server could not be reached. Please try again later.";

        public const string CODE_DATABASE_TRANSPORT_LEVEL_ERROR = "INFRA_DB_TRANSPORT_LEVEL_ERROR";
        public const string MSG_DATABASE_TRANSPORT_LEVEL_ERROR = "Infrastructure.Database.TransportLevelError";
        public const string FALLBACK_DATABASE_TRANSPORT_LEVEL_ERROR =
            "A network transport error occurred while contacting the database. Please try again later.";

        public const string CODE_DEFAULT_AVATAR_NOT_CONFIGURED = "INFRA_DEFAULT_AVATAR_NOT_CONFIGURED";
        public const string MSG_DEFAULT_AVATAR_NOT_CONFIGURED = "Infrastructure.Avatar.DefaultNotConfigured";
        public const string FALLBACK_DEFAULT_AVATAR_NOT_CONFIGURED =
            "The system is not correctly configured (default avatar is missing). Please try again later.";

        public const string CODE_REQUEST_NULL = "INFRA_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "Infrastructure.Request.Null";
        public const string FALLBACK_REQUEST_NULL =
            "The service received an empty request. Please verify your input parameters.";
    }
}
