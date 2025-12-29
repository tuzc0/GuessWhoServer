using System;

namespace GuessWhoServerDomain.Domain.Models.Friends
{
    public sealed class FriendRequestRecord
    {
        private const long INVALID_ID = 0;
        private const byte INVALID_STATUS_ID = 0;

        public long FriendRequestId { get; }
        public long RequesterUserId { get; }
        public long AddresseeUserId { get; }

        public string RequesterDisplayName { get; }
        public byte StatusId { get; }

        public DateTime CreatedAtUtc { get; }

        public bool IsValid => FriendRequestId > INVALID_ID;

        public FriendRequestRecord(
            long friendRequestId,
            long requesterUserId,
            long addresseeUserId,
            string requesterDisplayName,
            byte statusId,
            DateTime createdAtUtc)
        {
            FriendRequestId = friendRequestId;
            RequesterUserId = requesterUserId;
            AddresseeUserId = addresseeUserId;
            RequesterDisplayName = requesterDisplayName ?? string.Empty;
            StatusId = statusId;
            CreatedAtUtc = createdAtUtc;
        }

        public static FriendRequestRecord CreateInvalid()
        {
            return new FriendRequestRecord(
                INVALID_ID,
                INVALID_ID,
                INVALID_ID,
                string.Empty,
                INVALID_STATUS_ID,
                default);
        }
    }
}
