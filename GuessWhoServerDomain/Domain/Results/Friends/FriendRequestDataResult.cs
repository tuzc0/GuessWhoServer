using GuessWhoServerDomain.Domain.Enums.Friends;

namespace GuessWhoServerDomain.Domain.Results.Friends
{
    public sealed class FriendRequestDataResult
    {
        private FriendRequestDataResult(FriendRequestDataStatus status, long friendRequestId)
        {
            Status = status;
            FriendRequestId = friendRequestId;
        }

        public FriendRequestDataStatus Status { get; }

        public long FriendRequestId { get; }

        public static FriendRequestDataResult None()
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.None, 0);
        }

        public static FriendRequestDataResult NotFound()
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.NotFound, 0);
        }

        public static FriendRequestDataResult Created(long friendRequestId)
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.Created, friendRequestId);
        }

        public static FriendRequestDataResult ExistingPending(long friendRequestId)
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.ExistingPending, friendRequestId);
        }

        public static FriendRequestDataResult AutoAccepted(long friendRequestId)
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.AutoAccepted, friendRequestId);
        }

        public static FriendRequestDataResult Accepted(long friendRequestId)
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.Accepted, friendRequestId);
        }

        public static FriendRequestDataResult Rejected(long friendRequestId)
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.Rejected, friendRequestId);
        }

        public static FriendRequestDataResult Canceled(long friendRequestId)
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.Canceled, friendRequestId);
        }

        public static FriendRequestDataResult NotPending()
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.NotPending, 0);
        }

        public static FriendRequestDataResult NotAuthorized()
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.NotAuthorized, 0);
        }

        public static FriendRequestDataResult DestinationInactive()
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.DestinationInactive, 0);
        }

        public static FriendRequestDataResult AlreadyFriends()
        {
            return new FriendRequestDataResult(FriendRequestDataStatus.AlreadyFriends, 0);
        }
    }
}
