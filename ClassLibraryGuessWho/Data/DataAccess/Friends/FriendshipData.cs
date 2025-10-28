﻿using ClassLibraryGuessWho.Contracts;
using ClassLibraryGuessWho.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Friends
{
    public sealed class FriendshipData
    {

        public IList<UserProfileSearchResult> SearchProfilesByDisplayName(string displayName)
        {
            int maxResults = 10;
            IList<UserProfileSearchResult> profiles;

            using (var dataBaseContext = new GuessWhoDB())
            {

                profiles = dataBaseContext.USER_PROFILE
                    .AsNoTracking()
                    .Where(p => p.DISPLAYNAME.Contains(displayName) && p.ISACTIVE)
                    .OrderBy(p => p.DISPLAYNAME)
                    .Take(maxResults)
                    .Select(p => new UserProfileSearchResult
                    {
                        UserId = p.USERID,
                        DisplayName = p.DISPLAYNAME,
                        AvatarUrl = p.AVATAR.STORAGEURL
                    })
                    .ToList();
            }

            return profiles;
        }

        public bool AreAlreadyFriends(long userId1, long userId2)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                return dataBaseContext.FRIENDSHIP.Any(f =>
                    (f.USER1ID == userId1 && f.USER2ID == userId2) ||
                    (f.USER1ID == userId2 && f.USER2ID == userId1));
            }
        }

        public SendFriendRequestResponse TryAcceptInversePending(long fromUserId, long toUserId, DateTime timestampUtc)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                var inversePendingRequest = dataBaseContext.FRIEND_REQUEST.SingleOrDefault(fr =>
                    fr.REQUESTERUSERID == toUserId &&
                    fr.ADDRESSEEUSERID == fromUserId &&
                    fr.STATUSID == (byte)FriendRequestStatus.Pending);

                if (inversePendingRequest == null)
                {
                    return new SendFriendRequestResponse
                    {
                        Success = false,
                        AutoAccepted = false,
                        FriendRequestId = null,
                    };
                }

                checked
                {

                    long userIdLow = Math.Min(fromUserId, toUserId);
                    long userIdHigh = Math.Max(fromUserId, toUserId);

                    using (var transaction = dataBaseContext.Database.BeginTransaction())
                    {
                        inversePendingRequest.STATUSID = (byte)FriendRequestStatus.Accepted;
                        inversePendingRequest.RESPONDEDATUTC = timestampUtc;

                        dataBaseContext.FRIENDSHIP.Add(new FRIENDSHIP
                        {
                            USER1ID = inversePendingRequest.REQUESTERUSERID,
                            USER2ID = inversePendingRequest.ADDRESSEEUSERID,
                            USERIDLOW = userIdLow,
                            USERIDHIGH = userIdHigh,
                            CREATEDATUTC = timestampUtc
                        });

                        dataBaseContext.SaveChanges(); 
                        transaction.Commit();
                    }
                }

                return new SendFriendRequestResponse
                {
                    Success = true,
                    AutoAccepted = true,
                    FriendRequestId = inversePendingRequest.FRIENDREQUESTID.ToString()
                };
            }
        }

        public SendFriendRequestResponse TryReturnExistingPending(long fromUserId, long toUserId)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                var existingRequest = dataBaseContext.FRIEND_REQUEST
                    .SingleOrDefault(fr => fr.REQUESTERUSERID == fromUserId &&
                    fr.ADDRESSEEUSERID == toUserId && fr.STATUSID == (byte)FriendRequestStatus.Pending);

                if (existingRequest == null)
                {
                    return new SendFriendRequestResponse
                    {
                        Success = false,
                        AutoAccepted = false,
                        FriendRequestId = null
                    };
                }

                return new SendFriendRequestResponse
                {
                    Success = false,
                    AutoAccepted = false,
                    FriendRequestId = existingRequest.FRIENDREQUESTID.ToString()
                };
            }
        }

        public SendFriendRequestResponse CreateNewRequest(long fromUserId, long toUserId,DateTime timestampUtc)
        {
            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var newRequest = new FRIEND_REQUEST
                {
                    REQUESTERUSERID = fromUserId,
                    ADDRESSEEUSERID = toUserId,
                    STATUSID = (byte)FriendRequestStatus.Pending,
                    CREATEDATUTC = timestampUtc
                };

                dataBaseContext.FRIEND_REQUEST.Add(newRequest);
                dataBaseContext.SaveChanges();
                transaction.Commit();

                return new SendFriendRequestResponse
                {
                    Success = true,
                    AutoAccepted = false,
                    FriendRequestId = newRequest.FRIENDREQUESTID.ToString()
                };
            }
        }

        public void AcceptFriendRequest(long accountId, long friendRequestId, DateTime timestampUtc)
        {
            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var meUserId = ResolveUserIdFromAccountId(accountId);

                var request = dataBaseContext.FRIEND_REQUEST.SingleOrDefault(fr => fr.FRIENDREQUESTID == friendRequestId)
                          ?? throw new InvalidOperationException("Friend request not found.");

                if (request.STATUSID != (byte)FriendRequestStatus.Pending)
                {
                    throw new InvalidOperationException("Friend request is not pending.");
                }

                if (request.ADDRESSEEUSERID != meUserId)
                {
                    throw new InvalidOperationException("Not authorized to accept this request.");
                }

                var low = Math.Min(request.REQUESTERUSERID, request.ADDRESSEEUSERID);
                var high = Math.Max(request.REQUESTERUSERID, request.ADDRESSEEUSERID);
                var already = dataBaseContext.FRIENDSHIP.Any(f => f.USERIDLOW == low && f.USERIDHIGH == high);

                if (!already)
                {
                    dataBaseContext.FRIENDSHIP.Add(new FRIENDSHIP
                    {
                        USER1ID = request.REQUESTERUSERID,
                        USER2ID = request.ADDRESSEEUSERID,
                        USERIDLOW = low,
                        USERIDHIGH = high,
                        CREATEDATUTC = timestampUtc
                    });
                }

                request.STATUSID = (byte)FriendRequestStatus.Accepted;
                request.RESPONDEDATUTC = timestampUtc;

                dataBaseContext.SaveChanges();
                transaction.Commit();
            }
        }

        public void RejectFriendRequest(long accountId, long friendRequestId, DateTime timestampUtc)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                var meUserId = ResolveUserIdFromAccountId(accountId);

                var request = dataBaseContext.FRIEND_REQUEST.SingleOrDefault(fr => fr.FRIENDREQUESTID == friendRequestId)
                          ?? throw new InvalidOperationException("Friend request not found.");

                if (request.STATUSID != (byte)FriendRequestStatus.Pending)
                {
                    throw new InvalidOperationException("Friend request is not pending.");
                }

                if (request.ADDRESSEEUSERID != meUserId)
                {
                    throw new InvalidOperationException("Not authorized to reject this request.");
                }

                request.STATUSID = (byte)FriendRequestStatus.Rejected;
                request.RESPONDEDATUTC = timestampUtc;

                dataBaseContext.SaveChanges();
            }
        }

        public void CancelFriendRequest(long accountId, long friendRequestId, DateTime timestampUtc)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                var meUserId = ResolveUserIdFromAccountId(accountId);

                var request = dataBaseContext.FRIEND_REQUEST.SingleOrDefault(fr => fr.FRIENDREQUESTID == friendRequestId)
                          ?? throw new InvalidOperationException("Friend request not found.");

                if (request.STATUSID != (byte)FriendRequestStatus.Pending)
                {
                    throw new InvalidOperationException("Friend request is not pending.");
                }

                if (request.REQUESTERUSERID != meUserId)
                {
                    throw new InvalidOperationException("Not authorized to cancel this request.");
                }

                request.STATUSID = (byte)FriendRequestStatus.Canceled;
                request.RESPONDEDATUTC = timestampUtc;

                dataBaseContext.SaveChanges();
            }
        }

        public long ResolveUserIdFromAccountId(long accountId)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                var userId = dataBaseContext.ACCOUNT
                    .Where(a => a.ACCOUNTID == accountId)
                    .Select(a => a.USERID)
                    .SingleOrDefault();

                if (userId <= 0)
                {
                    throw new InvalidOperationException("Account does not exist.");
                }

                return userId;
            }
        }

        public void EnsureDestinationUserActive(long userId)
        {
            using (var dataBaseContext = new GuessWhoDB())
            {
                var isActive = dataBaseContext.USER_PROFILE
                    .Where(p => p.USERID == userId)
                    .Select(p => p.ISACTIVE)
                    .SingleOrDefault();

                if (!isActive)
                {
                    throw new InvalidOperationException("Destination user is not active.");
                }
            }
        }
    }
}
