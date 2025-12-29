using GuessWhoServerDomain.Domain.Enums.Friends;

namespace GuessWhoServerDomain.Domain.Results.Friends
{
    public sealed class SendFriendRequestDataResult
    {
        public FriendRequestDataStatus Status { get; }
        public long FriendRequestId { get; }
        public bool AutoAccepted { get; }
    }
}
