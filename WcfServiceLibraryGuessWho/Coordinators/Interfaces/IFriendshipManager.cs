using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IFriendshipManager
    {
        IList<UserProfileSearchRecord> GetFriends(long accountId);
        IList<FriendRequestRecord> GetPendingRequests(long accountId);
        IList<UserProfileSearchRecord> SearchProfiles(string displayName);
        SendFriendRequestResult SendFriendRequest(long fromAccountId, long toUserId, DateTime nowUtc);
        bool AcceptFriendRequest(FriendRequestActionArgs args);
        bool RejectFriendRequest(FriendRequestActionArgs args);
        bool CancelFriendRequest(FriendRequestActionArgs args);
    }

    public sealed class SendFriendRequestResult
    {
        private const long NO_ID = 0;

        public bool Success { get; }
        public bool AutoAccepted { get; }
        public long FriendRequestId { get; }

        private SendFriendRequestResult(bool success, bool autoAccepted, long friendRequestId)
        {
            Success = success;
            AutoAccepted = autoAccepted;
            FriendRequestId = friendRequestId;
        }

        public static SendFriendRequestResult OkCreated(long friendRequestId) =>
            new SendFriendRequestResult(true, false, friendRequestId);

        public static SendFriendRequestResult OkAutoAccepted() =>
            new SendFriendRequestResult(true, true, NO_ID);

        public static SendFriendRequestResult ExistingPending(long friendRequestId) =>
            new SendFriendRequestResult(false, false, friendRequestId);
    }
}