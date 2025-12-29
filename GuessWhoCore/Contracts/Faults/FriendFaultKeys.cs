namespace GuessWhoCore.Contracts.Faults
{
    public static class FriendFaultKeys
    {
        public const string CODE_REQUEST_NULL = "FRIEND_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "Friends.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "The request cannot be null.";

        public const string CODE_INVALID_ACCOUNT_ID = "FRIEND_INVALID_ACCOUNT_ID";
        public const string MSG_INVALID_ACCOUNT_ID = "Friends.InvalidAccountId";
        public const string FALLBACK_INVALID_ACCOUNT_ID = "Account ID is invalid.";

        public const string CODE_ACCOUNT_NOT_FOUND = "FRIEND_ACCOUNT_NOT_FOUND";
        public const string MSG_ACCOUNT_NOT_FOUND = "Friends.AccountNotFound";
        public const string FALLBACK_ACCOUNT_NOT_FOUND = "Account does not exist.";

        public const string CODE_INVALID_DISPLAY_NAME = "FRIEND_INVALID_DISPLAY_NAME";
        public const string MSG_INVALID_DISPLAY_NAME = "Friends.InvalidDisplayName";
        public const string FALLBACK_INVALID_DISPLAY_NAME = "Display name cannot be empty.";

        public const string CODE_INVALID_IDS = "FRIEND_INVALID_IDS";
        public const string MSG_INVALID_IDS = "Friends.InvalidIds";
        public const string FALLBACK_INVALID_IDS = "IDs must be positive.";

        public const string CODE_CANNOT_FRIEND_SELF = "FRIEND_CANNOT_SELF";
        public const string MSG_CANNOT_FRIEND_SELF = "Friends.CannotFriendSelf";
        public const string FALLBACK_CANNOT_FRIEND_SELF = "Cannot send a friend request to yourself.";

        public const string CODE_DESTINATION_INACTIVE = "FRIEND_DESTINATION_INACTIVE";
        public const string MSG_DESTINATION_INACTIVE = "Friends.DestinationInactive";
        public const string FALLBACK_DESTINATION_INACTIVE = "Destination user is not active.";

        public const string CODE_NOT_AUTHORIZED = "FRIEND_NOT_AUTHORIZED";
        public const string MSG_NOT_AUTHORIZED = "Friends.NotAuthorized";
        public const string FALLBACK_NOT_AUTHORIZED = "Not authorized to perform this action.";

        public const string CODE_NOT_FOUND = "FRIEND_REQUEST_NOT_FOUND";
        public const string MSG_NOT_FOUND = "Friends.RequestNotFound";
        public const string FALLBACK_NOT_FOUND = "Friend request not found.";

        public const string CODE_NOT_PENDING = "FRIEND_REQUEST_NOT_PENDING";
        public const string MSG_NOT_PENDING = "Friends.RequestNotPending";
        public const string FALLBACK_NOT_PENDING = "Friend request is not pending.";

        public const string CODE_ALREADY_FRIENDS = "FRIEND_ALREADY_FRIENDS";
        public const string MSG_ALREADY_FRIENDS = "Friends.AlreadyFriends";
        public const string FALLBACK_ALREADY_FRIENDS = "Users are already friends.";
    }
}