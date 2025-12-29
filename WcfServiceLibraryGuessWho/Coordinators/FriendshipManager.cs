using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Enums.Friends;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServerDomain.Domain.Results.Friends;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class FriendshipManager : ManagerBase, IFriendshipManager
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(FriendshipManager));

        private const string LOG_CTX_GET_FRIENDS = "FriendshipManager.GetFriends";
        private const string LOG_CTX_GET_PENDING = "FriendshipManager.GetPendingRequests";
        private const string LOG_CTX_SEARCH = "FriendshipManager.SearchProfiles";
        private const string LOG_CTX_SEND = "FriendshipManager.SendFriendRequest";
        private const string LOG_CTX_ACCEPT = "FriendshipManager.AcceptFriendRequest";
        private const string LOG_CTX_REJECT = "FriendshipManager.RejectFriendRequest";
        private const string LOG_CTX_CANCEL = "FriendshipManager.CancelFriendRequest";

        private const int MIN_VALID_ID = 1;

        private readonly IFriendshipRepository friendshipRepository;

        public FriendshipManager(IFriendshipRepository friendshipRepository)
        {
            this.friendshipRepository = friendshipRepository ??
                throw new ArgumentNullException(nameof(friendshipRepository));
        }

        public IList<UserProfileSearchRecord> GetFriends(string accountId)
        {
            return ExecuteService(
                LOG_CTX_GET_FRIENDS,
                () =>
                {
                    long parsedAccountId = ParseAccountIdOrThrow(accountId);

                    long userId = friendshipRepository.TryResolveUserIdFromAccountId(parsedAccountId);

                    EnsureUserIdFoundOrThrow(userId);

                    return friendshipRepository.GetFriends(userId) ?? new List<UserProfileSearchRecord>();
                });
        }

        public IList<FriendRequestRecord> GetPendingRequests(string accountId)
        {
            return ExecuteService(
                LOG_CTX_GET_PENDING,
                () =>
                {
                    long parsedAccountId = ParseAccountIdOrThrow(accountId);

                    long userId = friendshipRepository.TryResolveUserIdFromAccountId(parsedAccountId);

                    EnsureUserIdFoundOrThrow(userId);

                    return friendshipRepository.GetPendingRequests(userId) ?? new List<FriendRequestRecord>();
                });
        }

        public IList<UserProfileSearchRecord> SearchProfiles(string displayName)
        {
            return ExecuteService(
                LOG_CTX_SEARCH,
                () =>
                {
                    string trimmed = (displayName ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        throw FaultsFactory.Create(
                            FriendFaultKeys.CODE_INVALID_DISPLAY_NAME,
                            FriendFaultKeys.MSG_INVALID_DISPLAY_NAME,
                            FriendFaultKeys.FALLBACK_INVALID_DISPLAY_NAME);
                    }

                    return friendshipRepository.SearchProfilesByDisplayName(trimmed) ?? new List<UserProfileSearchRecord>();
                });
        }

        public SendFriendRequestResult SendFriendRequest(long fromAccountId, long toUserId, DateTime nowUtc)
        {
            return ExecuteService(
                LOG_CTX_SEND,
                () =>
                {
                    ValidateSendFriendRequestIdsOrThrow(fromAccountId, toUserId);

                    long fromUserId = ResolveFromUserIdOrThrow(fromAccountId);

                    EnsureNotSelfOrThrow(fromUserId, toUserId);
                    EnsureDestinationActiveOrThrow(toUserId);

                    if (friendshipRepository.AreAlreadyFriends(fromUserId, toUserId))
                    {
                        return SendFriendRequestResult.OkAutoAccepted();
                    }

                    SendFriendRequestResult result =
                        TryMap(friendshipRepository.TryAcceptInversePending(fromUserId, toUserId, nowUtc))
                        ?? TryMap(friendshipRepository.TryReturnExistingPending(fromUserId, toUserId))
                        ?? TryMap(friendshipRepository.CreateNewRequest(fromUserId, toUserId, nowUtc));

                    if (result != null)
                    {
                        return result;
                    }

                    throw FaultsFactory.Create(
                        FriendFaultKeys.CODE_ALREADY_FRIENDS,
                        FriendFaultKeys.MSG_ALREADY_FRIENDS,
                        FriendFaultKeys.FALLBACK_ALREADY_FRIENDS);
                });
        }

        private static void ValidateSendFriendRequestIdsOrThrow(long fromAccountId, long toUserId)
        {
            if (fromAccountId >= MIN_VALID_ID && toUserId >= MIN_VALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(
                FriendFaultKeys.CODE_INVALID_IDS,
                FriendFaultKeys.MSG_INVALID_IDS,
                FriendFaultKeys.FALLBACK_INVALID_IDS);
        }

        private long ResolveFromUserIdOrThrow(long fromAccountId)
        {
            long fromUserId = friendshipRepository.TryResolveUserIdFromAccountId(fromAccountId);
            EnsureUserIdFoundOrThrow(fromUserId);
            return fromUserId;
        }

        private static void EnsureNotSelfOrThrow(long fromUserId, long toUserId)
        {
            if (fromUserId != toUserId)
            {
                return;
            }

            throw FaultsFactory.Create(
                FriendFaultKeys.CODE_CANNOT_FRIEND_SELF,
                FriendFaultKeys.MSG_CANNOT_FRIEND_SELF,
                FriendFaultKeys.FALLBACK_CANNOT_FRIEND_SELF);
        }

        private void EnsureDestinationActiveOrThrow(long toUserId)
        {
            if (friendshipRepository.IsUserProfileActive(toUserId))
            {
                return;
            }

            throw FaultsFactory.Create(
                FriendFaultKeys.CODE_DESTINATION_INACTIVE,
                FriendFaultKeys.MSG_DESTINATION_INACTIVE,
                FriendFaultKeys.FALLBACK_DESTINATION_INACTIVE);
        }

        private static SendFriendRequestResult TryMap(FriendRequestDataResult dataResult)
        {
            if (dataResult == null)
            {
                return null;
            }

            switch (dataResult.Status)
            {
                case FriendRequestDataStatus.AutoAccepted:
                    return SendFriendRequestResult.OkCreated(dataResult.FriendRequestId);

                case FriendRequestDataStatus.ExistingPending:
                    return SendFriendRequestResult.ExistingPending(dataResult.FriendRequestId);

                case FriendRequestDataStatus.Created:
                    return SendFriendRequestResult.OkCreated(dataResult.FriendRequestId);

                case FriendRequestDataStatus.AlreadyFriends:
                    return SendFriendRequestResult.OkAutoAccepted();

                default:
                    return null;
            }
        }

        public bool AcceptFriendRequest(FriendRequestActionArgs args)
        {
            return ExecuteService(
                LOG_CTX_ACCEPT,
                () =>
                {
                    ValidateActionArgsOrThrow(args);

                    FriendRequestDataResult result = friendshipRepository.AcceptFriendRequest(args);

                    EnsureActionSucceededOrThrow(result);

                    return true;
                });
        }

        public bool RejectFriendRequest(FriendRequestActionArgs args)
        {
            return ExecuteService(
                LOG_CTX_REJECT,
                () =>
                {
                    ValidateActionArgsOrThrow(args);

                    FriendRequestDataResult result = friendshipRepository.RejectFriendRequest(args);

                    EnsureActionSucceededOrThrow(result);

                    return true;
                });
        }

        public bool CancelFriendRequest(FriendRequestActionArgs args)
        {
            return ExecuteService(
                LOG_CTX_CANCEL,
                () =>
                {
                    ValidateActionArgsOrThrow(args);

                    FriendRequestDataResult result = friendshipRepository.CancelFriendRequest(args);

                    EnsureActionSucceededOrThrow(result);

                    return true;
                });
        }

        private static long ParseAccountIdOrThrow(string accountId)
        {
            string trimmed = (accountId ?? string.Empty).Trim();

            if (!long.TryParse(trimmed, out long value) || value < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(
                    FriendFaultKeys.CODE_INVALID_ACCOUNT_ID,
                    FriendFaultKeys.MSG_INVALID_ACCOUNT_ID,
                    FriendFaultKeys.FALLBACK_INVALID_ACCOUNT_ID);
            }

            return value;
        }

        private static void EnsureUserIdFoundOrThrow(long userId)
        {
            if (userId >= MIN_VALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(
                FriendFaultKeys.CODE_ACCOUNT_NOT_FOUND,
                FriendFaultKeys.MSG_ACCOUNT_NOT_FOUND,
                FriendFaultKeys.FALLBACK_ACCOUNT_NOT_FOUND);
        }

        private static void ValidateActionArgsOrThrow(FriendRequestActionArgs args)
        {
            if (args == null || args.AccountId < MIN_VALID_ID || args.FriendRequestId < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(
                    FriendFaultKeys.CODE_INVALID_IDS,
                    FriendFaultKeys.MSG_INVALID_IDS,
                    FriendFaultKeys.FALLBACK_INVALID_IDS);
            }
        }

        private static void EnsureActionSucceededOrThrow(FriendRequestDataResult result)
        {
            if (result == null)
            {
                throw FaultsFactory.Create(
                    FriendFaultKeys.CODE_NOT_FOUND,
                    FriendFaultKeys.MSG_NOT_FOUND,
                    FriendFaultKeys.FALLBACK_NOT_FOUND);
            }

            switch (result.Status)
            {
                case FriendRequestDataStatus.Accepted:
                case FriendRequestDataStatus.Rejected:
                case FriendRequestDataStatus.Canceled:
                    return;

                case FriendRequestDataStatus.NotFound:
                    throw FaultsFactory.Create(
                        FriendFaultKeys.CODE_NOT_FOUND,
                        FriendFaultKeys.MSG_NOT_FOUND,
                        FriendFaultKeys.FALLBACK_NOT_FOUND);

                case FriendRequestDataStatus.NotPending:
                    throw FaultsFactory.Create(
                        FriendFaultKeys.CODE_NOT_PENDING,
                        FriendFaultKeys.MSG_NOT_PENDING,
                        FriendFaultKeys.FALLBACK_NOT_PENDING);

                case FriendRequestDataStatus.NotAuthorized:
                    throw FaultsFactory.Create(
                        FriendFaultKeys.CODE_NOT_AUTHORIZED,
                        FriendFaultKeys.MSG_NOT_AUTHORIZED,
                        FriendFaultKeys.FALLBACK_NOT_AUTHORIZED);

                default:
                    throw FaultsFactory.Create(
                        FriendFaultKeys.CODE_ALREADY_FRIENDS,
                        FriendFaultKeys.MSG_ALREADY_FRIENDS,
                        FriendFaultKeys.FALLBACK_ALREADY_FRIENDS);
            }
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
