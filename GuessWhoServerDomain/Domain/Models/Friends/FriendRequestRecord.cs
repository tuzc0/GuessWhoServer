using System;

namespace GuessWhoServerDomain.Domain.Models.Friends
{
    public sealed class FriendRequestRecord
    {
        private const long INVALID_ID = 0;
        private const byte INVALID_STATUS_ID = 0;

        public long FriendRequestId { get; set; }
        public long RequesterUserId { get; set; }
        public long AddresseeUserId { get; set; }
        public string RequesterDisplayName { get; set; }
        public byte StatusId { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public bool IsValid => FriendRequestId > INVALID_ID;

        public FriendRequestRecord() { }

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
            return new FriendRequestRecord
            {
                FriendRequestId = INVALID_ID,
                RequesterUserId = INVALID_ID,
                AddresseeUserId = INVALID_ID,
                RequesterDisplayName = string.Empty,
                StatusId = INVALID_STATUS_ID,
                CreatedAtUtc = default
            };
        }
    }
}