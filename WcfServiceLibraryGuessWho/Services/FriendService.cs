using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Request;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;

namespace GuessWhoServices.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class FriendService : ServiceBase, IFriendService
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(FriendService));

        private const string LOG_CTX_SEARCH = "FriendService.SearchProfiles";
        private const string LOG_CTX_SEND = "FriendService.SendFriendRequest";
        private const string LOG_CTX_ACCEPT = "FriendService.AcceptFriendRequest";
        private const string LOG_CTX_REJECT = "FriendService.RejectFriendRequest";
        private const string LOG_CTX_CANCEL = "FriendService.CancelFriendRequest";
        private const string LOG_CTX_GET_FRIENDS = "FriendService.GetFriends";
        private const string LOG_CTX_GET_PENDING = "FriendService.GetPendingRequests";

        private const string EMPTY = "";
        private const int MIN_VALID_ID = 1;

        private readonly IFriendshipManager friendshipManager;

        public FriendService(IFriendshipManager friendshipManager)
        {
            this.friendshipManager = friendshipManager ??
                throw new ArgumentNullException(nameof(friendshipManager));
        }

        public SearchProfilesResponse SearchProfiles(SearchProfileRequest request)
        {
            return ExecuteService(
                LOG_CTX_SEARCH,
                () =>
                {
                    EnsureRequestNotNull(request);

                    IList<UserProfileSearchRecord> profiles =
                        friendshipManager.SearchProfiles(request.DisplayName);

                    return new SearchProfilesResponse
                    {
                        Profiles = MapProfiles(profiles)
                    };
                });
        }

        public SendFriendRequestResponse SendFriendRequest(SendFriendRequestRequest request)
        {
            return ExecuteService(
                LOG_CTX_SEND,
                () =>
                {
                    EnsureRequestNotNull(request);

                    ValidateIdOrThrow(request.FromAccountId, FriendFaultKeys.CODE_INVALID_ACCOUNT_ID);
                    ValidateIdOrThrow(request.ToUserId, FriendFaultKeys.CODE_INVALID_IDS);

                    var result = friendshipManager.SendFriendRequest(
                        request.FromAccountId,
                        request.ToUserId,
                        DateTime.UtcNow);

                    if (result == null)
                    {
                        return new SendFriendRequestResponse
                        {
                            Success = false,
                            AutoAccepted = false,
                            FriendRequestId = EMPTY
                        };
                    }

                    return new SendFriendRequestResponse
                    {
                        Success = result.Success,
                        AutoAccepted = result.AutoAccepted,
                        FriendRequestId = result.FriendRequestId ?? EMPTY
                    };
                });
        }

        public BasicResponse AcceptFriendRequest(FriendRequestOperationRequest request)
        {
            return ExecuteService(
                LOG_CTX_ACCEPT,
                () =>
                {
                    EnsureRequestNotNull(request);
                    FriendRequestActionArgs args = BuildActionArgs(request);
                    bool success = friendshipManager.AcceptFriendRequest(args);

                    return new BasicResponse { Success = success };
                });
        }

        public BasicResponse RejectFriendRequest(FriendRequestOperationRequest request)
        {
            return ExecuteService(
                LOG_CTX_REJECT,
                () =>
                {
                    EnsureRequestNotNull(request);
                    FriendRequestActionArgs args = BuildActionArgs(request);
                    bool success = friendshipManager.RejectFriendRequest(args);

                    return new BasicResponse { Success = success };
                });
        }

        public BasicResponse CancelFriendRequest(FriendRequestOperationRequest request)
        {
            return ExecuteService(
                LOG_CTX_CANCEL,
                () =>
                {
                    EnsureRequestNotNull(request);
                    FriendRequestActionArgs args = BuildActionArgs(request);
                    bool success = friendshipManager.CancelFriendRequest(args);

                    return new BasicResponse { Success = success };
                });
        }

        public GetFriendsResponse GetFriends(GetFriendsRequest request)
        {
            return ExecuteService(
                LOG_CTX_GET_FRIENDS,
                () =>
                {
                    EnsureRequestNotNull(request);
                    ValidateIdOrThrow(request.AccountId, FriendFaultKeys.CODE_INVALID_ACCOUNT_ID);

                    IList<UserProfileSearchRecord> friends = friendshipManager.GetFriends(request.AccountId);

                    return new GetFriendsResponse
                    {
                        Friends = MapProfiles(friends)
                    };
                });
        }

        public GetPendingRequestsResponse GetPendingRequests(GetPendingFriendRequestsRequest request)
        {
            return ExecuteService(
                LOG_CTX_GET_PENDING,
                () =>
                {
                    EnsureRequestNotNull(request);
                    ValidateIdOrThrow(request.AccountId, FriendFaultKeys.CODE_INVALID_ACCOUNT_ID);

                    IList<FriendRequestRecord> records = friendshipManager.GetPendingRequests(request.AccountId);

                    return new GetPendingRequestsResponse
                    {
                        Requests = MapFriendRequests(records)
                    };
                });
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(FriendFaultKeys.CODE_REQUEST_NULL);
            }
        }

        private static FriendRequestActionArgs BuildActionArgs(FriendRequestOperationRequest request)
        {
            ValidateIdOrThrow(request.AccountId, FriendFaultKeys.CODE_INVALID_ACCOUNT_ID);
            ValidateIdOrThrow(request.FriendRequestId, FriendFaultKeys.CODE_INVALID_IDS);

            return new FriendRequestActionArgs(request.AccountId, request.FriendRequestId, DateTime.UtcNow);
        }

        private static void ValidateIdOrThrow(long id, string code)
        {
            if (id < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(code);
            }
        }

        private static List<UserProfileSearchResult> MapProfiles(IList<UserProfileSearchRecord> profiles)
        {
            if (profiles == null) return new List<UserProfileSearchResult>();

            return profiles.Select(p => new UserProfileSearchResult
            {
                UserId = p.UserId,
                DisplayName = p.DisplayName ?? EMPTY,
                AvatarId = p.AvatarId ?? EMPTY
            }).ToList();
        }

        private static List<FriendRequest> MapFriendRequests(IList<FriendRequestRecord> records)
        {
            if (records == null) return new List<FriendRequest>();

            return records.Select(r => new FriendRequest
            {
                FriendRequestId = r.FriendRequestId,
                RequesterUserId = r.RequesterUserId,
                RequesterDisplayName = r.RequesterDisplayName ?? EMPTY,
                AddresseeUserId = r.AddresseeUserId,
                StatusId = r.StatusId,
                CreatedAt = r.CreatedAtUtc
            }).ToList();
        }
    }
}