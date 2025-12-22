namespace WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs
{
    internal static class UserServiceFaults
    {
        internal const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        internal const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT = "The server took too long to respond. Please try again.";

        internal const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";
        internal const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE = "The server could not connect to the database. Please try again later.";

        internal const string FAULT_CODE_UNEXPECTED_ERROR = "USER_UNEXPECTED_ERROR";
        internal const string FAULT_MESSAGE_UNEXPECTED_ERROR = "An unexpected error occurred while processing the request. Please try again later";
    }
}
