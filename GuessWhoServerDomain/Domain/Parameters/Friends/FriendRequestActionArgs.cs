using System;

namespace GuessWhoServerDomain.Domain.Parameters.Friends
{
    public sealed class FriendRequestActionArgs
    {
        public long AccountId { get; }
        public long FriendRequestId { get; }
        public DateTime NowUtc { get; }

        public FriendRequestActionArgs(long accountId, long friendRequestId, DateTime nowUtc)
        {
            AccountId = accountId;
            FriendRequestId = friendRequestId;
            NowUtc = nowUtc;
        }
    }
}
