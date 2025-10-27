using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Contracts.Services;
using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Friends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace WcfServiceLibraryGuessWho.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class FriendService : IFriendService
    {
        private readonly FriendshipData friendshipData = new FriendshipData();
        public SearchProfilesResponse SearchProfiles(SearchProfileRequest request)
        {

            EnsureRequestNotNull(request);

            var displayName = (request.DisplayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw Faults.Create("InvalidDisplayName", "Display name cannot be empty.");
            }

            try
            {
                var profiles = friendshipData.SearchProfilesByDisplayName(displayName);
                return new SearchProfilesResponse
                {
                    Profiles = profiles.ToList()
                };
            }
            catch (InvalidOperationException ex)
            {
                throw Faults.Create("SearchProfilesError", "An error occurred while searching for profiles: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw Faults.Create("SearchProfilesError", "An unexpected error occurred while searching for profiles: " + ex.Message);
            }
        }

        public SendFriendRequestResponse SendFriendRequest(SendFriendRequestRequest request)
        {

            EnsureRequestNotNull(request);

            if (request.FromAccountId <= 0 || request.ToUserId <= 0)
            {
                throw Faults.Create("InvalidAccountId", "Account IDs must be positive.");
            }

            try
            {
                var dateTimeNow = DateTime.UtcNow;
                var fromUserId = friendshipData.ResolveUserIdFromAccountId(request.FromAccountId);

                if (fromUserId == request.ToUserId)
                {
                    throw Faults.Create("InvalidFriendRequest", "Cannot send friend request to oneself.");
                }

                friendshipData.EnsureDestinationUserActive(request.ToUserId);

                if (friendshipData.AreAlreadyFriends(fromUserId, request.ToUserId))
                {
                    return new SendFriendRequestResponse
                    {
                        Success = true,
                        AutoAccepted = true,
                        FriendRequestId = null
                    };
                }

                var inverseRequestExisting = friendshipData.TryAcceptInversePending(fromUserId, request.ToUserId, dateTimeNow);

                if (inverseRequestExisting != null)
                {
                    return inverseRequestExisting;
                }

                var existingRequest = friendshipData.TryReturnExistingPending(fromUserId, request.ToUserId);

                if (existingRequest != null)
                {
                    return existingRequest;
                }

                return friendshipData.CreateNewRequest(fromUserId, request.ToUserId, dateTimeNow);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw Faults.Create("FriendRequestError", ex.Message);
            }
            catch (Exception ex)
            {
                throw Faults.Create("SendFriendRequestError", "An unexpected error occurred while sending friend request: " + ex.Message);
            }
        }

        public BasicResponse AcceptFriendRequest(AcceptFriendRequestRequest request)
        {

            EnsureRequestNotNull(request);
            var(accountId, friendRequestId) = validateIdsOrFault(request);

            try
            {
                friendshipData.AcceptFriendRequest(accountId, friendRequestId, DateTime.UtcNow);
                return new BasicResponse { Success = true };
            }
            catch (InvalidOperationException ex)
            {
                throw Faults.Create("AcceptFriendRequestError", ex.Message);
            }
            catch (Exception ex)
            {
                throw Faults.Create("AcceptFriendRequestError", "An unexpected error occurred while accepting friend request: " + ex.Message);
            }
        }

        public BasicResponse RejectFriendRequest(AcceptFriendRequestRequest request)
        {

            EnsureRequestNotNull(request);
            var (accountId, friendRequestId) = validateIdsOrFault(request);

            try
            {
                friendshipData.RejectFriendRequest(accountId, friendRequestId, DateTime.UtcNow);
                return new BasicResponse { Success = true };
            }
            catch (InvalidOperationException ex)
            {
                throw Faults.Create("RejectFriendRequestError", ex.Message);
            }
            catch (Exception ex)
            {
                throw Faults.Create("RejectFriendRequestError", "An unexpected error occurred while rejecting friend request: " + ex.Message);
            }
        }

        public BasicResponse CancelFriendRequest(AcceptFriendRequestRequest request)
        {

            EnsureRequestNotNull(request);
            var (accountId, friendRequestId) = validateIdsOrFault(request);
            try
            {
                friendshipData.CancelFriendRequest(accountId, friendRequestId, DateTime.UtcNow);
                return new BasicResponse { Success = true };
            }
            catch (InvalidOperationException ex)
            {
                throw Faults.Create("CancelFriendRequestError", ex.Message);
            }
            catch (Exception ex)
            {
                throw Faults.Create("CancelFriendRequestError", "An unexpected error occurred while cancelling friend request: " + ex.Message);
            }
        }

        private static void EnsureRequestNotNull<T>(T request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Request cannot be null.");
            }
        }

        private static (long AccountId, long friendRequestId) validateIdsOrFault(AcceptFriendRequestRequest request)
        {
            EnsureRequestNotNull(request);

            if (!long.TryParse(request.AccountId, out var accountId) || accountId <= 0)
                throw Faults.Create("InvalidAccountId", "AccountId is invalid.");

            if (!long.TryParse(request.FriendRequestId, out var friendRequestId) || friendRequestId <= 0)
                throw Faults.Create("InvalidFriendRequestId", "FriendRequestId is invalid.");

            return (accountId, friendRequestId);

        }
    }
}
