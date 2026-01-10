namespace GuessWhoCore.Contracts.Faults
{
    public static class InfrastructureFaultKeys
    {
        public const string CODE_UNEXPECTED_ERROR = "INFRA_UNEXPECTED_ERROR";
        public const string CODE_DATABASE_COMMAND_TIMEOUT = "INFRA_DB_COMMAND_TIMEOUT";
        public const string CODE_DATABASE_CONNECTION_FAILURE = "INFRA_DB_CONNECTION_FAILURE";
        public const string CODE_DATABASE_NOT_FOUND = "INFRA_DB_NOT_FOUND";
        public const string CODE_DATABASE_NETWORK_PATH_NOT_FOUND = "INFRA_DB_NETWORK_PATH_NOT_FOUND";
        public const string CODE_DATABASE_UNREACHABLE = "INFRA_DB_UNREACHABLE";
        public const string CODE_DATABASE_SERVER_NOT_FOUND = "INFRA_DB_SERVER_NOT_FOUND";
        public const string CODE_DATABASE_TRANSPORT_LEVEL_ERROR = "INFRA_DB_TRANSPORT_LEVEL_ERROR";
        public const string CODE_DEFAULT_AVATAR_NOT_CONFIGURED = "INFRA_DEFAULT_AVATAR_NOT_CONFIGURED";
        public const string CODE_REQUEST_NULL = "INFRA_REQUEST_NULL";
    }
}