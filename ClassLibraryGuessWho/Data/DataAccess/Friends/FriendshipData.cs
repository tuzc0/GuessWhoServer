using GuessWhoServerDomain.Domain.Enums.Friends;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServerDomain.Domain.Results.Friends;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Friends
{
    public sealed class FriendshipData : IFriendshipRepository
    {
        private const int MAX_PROFILE_SEARCH_RESULTS = 10;
        private const int MIN_VALID_ID = 1;

        private const byte FRIEND_REQUEST_STATUS_PENDING = (byte)FriendRequestStatus.Pending;
        private const byte FRIEND_REQUEST_STATUS_ACCEPTED = (byte)FriendRequestStatus.Accepted;
        private const byte FRIEND_REQUEST_STATUS_REJECTED = (byte)FriendRequestStatus.Rejected;
        private const byte FRIEND_REQUEST_STATUS_CANCELED = (byte)FriendRequestStatus.Canceled;

        private readonly GuessWhoDBEntities dataContext;

        public FriendshipData(GuessWhoDBEntities dataContext)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public IList<UserProfileSearchRecord> SearchProfilesByDisplayName(string displayName)
        {
            string trimmed = (displayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return new List<UserProfileSearchRecord>();
            }

            return (from p in dataContext.USER_PROFILE.AsNoTracking()
                    join a in dataContext.ACCOUNT.AsNoTracking()
                        on p.USERID equals a.USERID
                    where p.ISACTIVE
                          && !p.ISGUEST
                          && !a.ISDELETED
                          && p.DISPLAYNAME.Contains(trimmed)
                    orderby p.DISPLAYNAME
                    select new UserProfileSearchRecord
                    {
                        UserId = p.USERID,
                        DisplayName = p.DISPLAYNAME,
                        AvatarId = p.AVATARID
                    })
                    .Take(MAX_PROFILE_SEARCH_RESULTS)
                    .ToList();
        }

        public bool AreAlreadyFriends(long userId1, long userId2)
        {
            long userIdLow = Math.Min(userId1, userId2);
            long userIdHigh = Math.Max(userId1, userId2);

            return dataContext.FRIENDSHIP.Any(f => f.USERIDLOW == userIdLow && f.USERIDHIGH == userIdHigh);
        }

        public FriendRequestDataResult TryAcceptInversePending(long fromUserId, long toUserId, DateTime timestampUtc)
        {
            FRIEND_REQUEST inversePendingRequest = dataContext.FRIEND_REQUEST
                .SingleOrDefault(fr =>
                    fr.REQUESTERUSERID == toUserId &&
                    fr.ADDRESSEEUSERID == fromUserId &&
                    fr.STATUSID == FRIEND_REQUEST_STATUS_PENDING);

            if (inversePendingRequest == null)
            {
                return FriendRequestDataResult.NotFound();
            }

            EnsureFriendshipExists(
                inversePendingRequest.REQUESTERUSERID,
                inversePendingRequest.ADDRESSEEUSERID,
                timestampUtc);

            inversePendingRequest.STATUSID = FRIEND_REQUEST_STATUS_ACCEPTED;
            inversePendingRequest.RESPONDEDATUTC = timestampUtc;

            return FriendRequestDataResult.AutoAccepted(inversePendingRequest.FRIENDREQUESTID);
        }

        public FriendRequestDataResult TryReturnExistingPending(long fromUserId, long toUserId)
        {
            FRIEND_REQUEST existingRequest = dataContext.FRIEND_REQUEST
                .SingleOrDefault(fr =>
                    fr.REQUESTERUSERID == fromUserId &&
                    fr.ADDRESSEEUSERID == toUserId &&
                    fr.STATUSID == FRIEND_REQUEST_STATUS_PENDING);

            if (existingRequest == null)
            {
                return FriendRequestDataResult.NotFound();
            }

            return FriendRequestDataResult.ExistingPending(existingRequest.FRIENDREQUESTID);
        }

        public IFriendRequestIdProvider CreateNewRequest(long fromUserId, long toUserId, DateTime timestampUtc)
        {
            var newRequest = new FRIEND_REQUEST
            {
                REQUESTERUSERID = fromUserId,
                ADDRESSEEUSERID = toUserId,
                STATUSID = FRIEND_REQUEST_STATUS_PENDING,
                CREATEDATUTC = timestampUtc
            };

            dataContext.FRIEND_REQUEST.Add(newRequest);

            return new FriendRequestIdProvider(newRequest);
        }

        public FriendRequestDataResult AcceptFriendRequest(FriendRequestActionArgs args)
        {
            FRIEND_REQUEST request = dataContext.FRIEND_REQUEST
                .SingleOrDefault(fr => fr.FRIENDREQUESTID == args.FriendRequestId);

            if (request == null)
            {
                return FriendRequestDataResult.NotFound();
            }

            if (request.STATUSID != FRIEND_REQUEST_STATUS_PENDING)
            {
                return FriendRequestDataResult.NotPending();
            }

            long meUserId = TryResolveUserIdFromAccountId(args.AccountId);

            if (request.ADDRESSEEUSERID != meUserId)
            {
                return FriendRequestDataResult.NotAuthorized();
            }

            EnsureFriendshipExists(request.REQUESTERUSERID, request.ADDRESSEEUSERID, args.NowUtc);

            request.STATUSID = FRIEND_REQUEST_STATUS_ACCEPTED;
            request.RESPONDEDATUTC = args.NowUtc;

            return FriendRequestDataResult.Accepted(args.FriendRequestId);
        }

        public FriendRequestDataResult RejectFriendRequest(FriendRequestActionArgs args)
        {
            FRIEND_REQUEST request = dataContext.FRIEND_REQUEST
                .SingleOrDefault(fr => fr.FRIENDREQUESTID == args.FriendRequestId);

            if (request == null)
            {
                return FriendRequestDataResult.NotFound();
            }

            if (request.STATUSID != FRIEND_REQUEST_STATUS_PENDING)
            {
                return FriendRequestDataResult.NotPending();
            }

            long meUserId = TryResolveUserIdFromAccountId(args.AccountId);

            if (request.ADDRESSEEUSERID != meUserId)
            {
                return FriendRequestDataResult.NotAuthorized();
            }

            request.STATUSID = FRIEND_REQUEST_STATUS_REJECTED;
            request.RESPONDEDATUTC = args.NowUtc;

            return FriendRequestDataResult.Rejected(args.FriendRequestId);
        }

        public FriendRequestDataResult CancelFriendRequest(FriendRequestActionArgs args)
        {
            FRIEND_REQUEST request = dataContext.FRIEND_REQUEST
                .SingleOrDefault(fr => fr.FRIENDREQUESTID == args.FriendRequestId);

            if (request == null)
            {
                return FriendRequestDataResult.NotFound();
            }

            if (request.STATUSID != FRIEND_REQUEST_STATUS_PENDING)
            {
                return FriendRequestDataResult.NotPending();
            }

            long meUserId = TryResolveUserIdFromAccountId(args.AccountId);

            if (request.REQUESTERUSERID != meUserId)
            {
                return FriendRequestDataResult.NotAuthorized();
            }

            request.STATUSID = FRIEND_REQUEST_STATUS_CANCELED;
            request.RESPONDEDATUTC = args.NowUtc;

            return FriendRequestDataResult.Canceled(args.FriendRequestId);
        }

        public long TryResolveUserIdFromAccountId(long accountId)
        {
            return (from a in dataContext.ACCOUNT.AsNoTracking()
                    join p in dataContext.USER_PROFILE.AsNoTracking()
                        on a.USERID equals p.USERID
                    where a.ACCOUNTID == accountId
                          && !a.ISDELETED
                          && p.ISACTIVE
                          && !p.ISGUEST
                    select a.USERID
                    ).SingleOrDefault();
        }

        public bool IsUserProfileActive(long userId)
        {
            return dataContext.USER_PROFILE
                .AsNoTracking()
                .Any(p => p.USERID == userId && p.ISACTIVE && !p.ISGUEST);
        }

        public IList<UserProfileSearchRecord> GetFriends(long userId)
        {
            var friendsWhereIAmFirst =
                from f in dataContext.FRIENDSHIP.AsNoTracking()
                where f.USER1ID == userId
                join p in dataContext.USER_PROFILE.AsNoTracking()
                    on f.USER2ID equals p.USERID
                where p.ISACTIVE && !p.ISGUEST
                select new UserProfileSearchRecord
                {
                    UserId = p.USERID,
                    DisplayName = p.DISPLAYNAME,
                    AvatarId = p.AVATARID
                };

            var friendsWhereIAmSecond =
                from f in dataContext.FRIENDSHIP.AsNoTracking()
                where f.USER2ID == userId
                join p in dataContext.USER_PROFILE.AsNoTracking()
                    on f.USER1ID equals p.USERID
                where p.ISACTIVE && !p.ISGUEST
                select new UserProfileSearchRecord
                {
                    UserId = p.USERID,
                    DisplayName = p.DISPLAYNAME,
                    AvatarId = p.AVATARID
                };

            return friendsWhereIAmFirst.Concat(friendsWhereIAmSecond).ToList();
        }

        public IList<FriendRequestRecord> GetPendingRequests(long userId)
        {
            if (userId < MIN_VALID_ID)
            {
                return new List<FriendRequestRecord>();
            }

            return (
                from fr in dataContext.FRIEND_REQUEST.AsNoTracking()
                join p in dataContext.USER_PROFILE.AsNoTracking()
                    on fr.REQUESTERUSERID equals p.USERID
                where fr.ADDRESSEEUSERID == userId
                      && fr.STATUSID == FRIEND_REQUEST_STATUS_PENDING
                      && p.ISACTIVE
                      && !p.ISGUEST
                select new FriendRequestRecord
                {
                    FriendRequestId = fr.FRIENDREQUESTID,
                    RequesterUserId = fr.REQUESTERUSERID,
                    AddresseeUserId = fr.ADDRESSEEUSERID,
                    RequesterDisplayName = p.DISPLAYNAME,
                    StatusId = fr.STATUSID,
                    CreatedAtUtc = fr.CREATEDATUTC
                }
            ).ToList();
        }

        public IList<FriendRequestRecord> GetSentRequests(long userId)
        {
            if (userId < MIN_VALID_ID)
            {
                return new List<FriendRequestRecord>();
            }

            return (
                from fr in dataContext.FRIEND_REQUEST.AsNoTracking()
                join p in dataContext.USER_PROFILE.AsNoTracking()
                    on fr.ADDRESSEEUSERID equals p.USERID
                where fr.REQUESTERUSERID == userId
                      && fr.STATUSID == FRIEND_REQUEST_STATUS_PENDING
                      && p.ISACTIVE
                      && !p.ISGUEST
                select new FriendRequestRecord
                {
                    FriendRequestId = fr.FRIENDREQUESTID,
                    RequesterUserId = fr.REQUESTERUSERID,
                    AddresseeUserId = fr.ADDRESSEEUSERID,
                    RequesterDisplayName = p.DISPLAYNAME,
                    StatusId = fr.STATUSID,
                    CreatedAtUtc = fr.CREATEDATUTC
                }
            ).ToList();
        }

        private void EnsureFriendshipExists(long user1Id, long user2Id, DateTime createdAtUtc)
        {
            long low = Math.Min(user1Id, user2Id);
            long high = Math.Max(user1Id, user2Id);

            bool exists = dataContext.FRIENDSHIP.Any(f => f.USERIDLOW == low && f.USERIDHIGH == high);

            if (exists)
            {
                return;
            }

            dataContext.FRIENDSHIP.Add(new FRIENDSHIP
            {
                USER1ID = user1Id,
                USER2ID = user2Id,
                CREATEDATUTC = createdAtUtc
            });
        }

        private sealed class FriendRequestIdProvider : IFriendRequestIdProvider
        {
            private readonly FRIEND_REQUEST friendRequest;

            public FriendRequestIdProvider(FRIEND_REQUEST friendRequest)
            {
                this.friendRequest = friendRequest ?? throw new ArgumentNullException(nameof(friendRequest));
            }

            public long FriendRequestId => friendRequest.FRIENDREQUESTID;
        }
    }
}