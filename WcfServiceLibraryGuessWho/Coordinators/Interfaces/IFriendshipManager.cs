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
        public bool Success { get; }
        public bool AutoAccepted { get; }
        public string FriendRequestId { get; }

        private SendFriendRequestResult(bool success, bool autoAccepted, string friendRequestId)
        {
            Success = success;
            AutoAccepted = autoAccepted;
            FriendRequestId = friendRequestId ?? string.Empty;
        }

        public static SendFriendRequestResult OkCreated(long friendRequestId) =>
            new SendFriendRequestResult(true, false, friendRequestId.ToString());

        public static SendFriendRequestResult OkAutoAccepted() =>
            new SendFriendRequestResult(true, true, string.Empty);

        public static SendFriendRequestResult ExistingPending(long friendRequestId) =>
            new SendFriendRequestResult(false, false, friendRequestId.ToString());
    }
}