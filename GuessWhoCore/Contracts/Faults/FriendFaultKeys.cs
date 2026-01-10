namespace GuessWhoCore.Contracts.Faults
{
    public static class FriendFaultKeys
    {
        public const string CODE_REQUEST_NULL = "FRIEND_REQUEST_NULL";
        public const string CODE_INVALID_ACCOUNT_ID = "FRIEND_INVALID_ACCOUNT_ID";
        public const string CODE_ACCOUNT_NOT_FOUND = "FRIEND_ACCOUNT_NOT_FOUND";
        public const string CODE_INVALID_DISPLAY_NAME = "FRIEND_INVALID_DISPLAY_NAME";
        public const string CODE_INVALID_IDS = "FRIEND_INVALID_IDS";
        public const string CODE_CANNOT_FRIEND_SELF = "FRIEND_CANNOT_SELF";
        public const string CODE_DESTINATION_INACTIVE = "FRIEND_DESTINATION_INACTIVE";
        public const string CODE_NOT_AUTHORIZED = "FRIEND_NOT_AUTHORIZED";
        public const string CODE_NOT_FOUND = "FRIEND_REQUEST_NOT_FOUND";
        public const string CODE_NOT_PENDING = "FRIEND_REQUEST_NOT_PENDING";
        public const string CODE_ALREADY_FRIENDS = "FRIEND_ALREADY_FRIENDS";
        public const string CODE_UNEXPECTED_ERROR = "FRIEND_UNEXPECTED_ERROR";
        public const string CODE_REQUEST_ID_NOT_GENERATED = "FRIEND_REQUEST_ID_NOT_GENERATED";
        public const string CODE_REQUEST_ALREADY_PENDING = "FRIEND_REQUEST_ALREADY_PENDING";
    }
}
