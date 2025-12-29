using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServerDomain.Domain.Results.Friends;
using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IFriendshipRepository
    {
        IList<UserProfileSearchRecord> SearchProfilesByDisplayName(string displayName);

        bool AreAlreadyFriends(long userId1, long userId2);

        FriendRequestDataResult TryAcceptInversePending(long fromUserId, long toUserId, DateTime timestampUtc);

        FriendRequestDataResult TryReturnExistingPending(long fromUserId, long toUserId);

        FriendRequestDataResult CreateNewRequest(long fromUserId, long toUserId, DateTime timestampUtc);

        FriendRequestDataResult AcceptFriendRequest(FriendRequestActionArgs args);

        FriendRequestDataResult RejectFriendRequest(FriendRequestActionArgs args);

        FriendRequestDataResult CancelFriendRequest(FriendRequestActionArgs args);

        long TryResolveUserIdFromAccountId(long accountId);

        bool IsUserProfileActive(long userId);

        IList<UserProfileSearchRecord> GetFriends(long userId);

        IList<FriendRequestRecord> GetPendingRequests(long userId);

        IList<FriendRequestRecord> GetSentRequests(long userId);
    }
}
