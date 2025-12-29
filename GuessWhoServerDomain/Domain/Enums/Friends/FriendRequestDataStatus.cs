namespace GuessWhoServerDomain.Domain.Enums.Friends
{
    public enum FriendRequestDataStatus
    {
        None = 0,

        Created = 1,
        ExistingPending = 2,
        AutoAccepted = 3,

        Accepted = 10,
        Rejected = 11,
        Canceled = 12,

        NotFound = 20,
        NotPending = 21,
        NotAuthorized = 22,
        DestinationInactive = 23,

        AlreadyFriends = 30
    }
}
