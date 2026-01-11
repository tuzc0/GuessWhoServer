using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Enums.Friends;
using GuessWhoServerDomain.Domain.Models.Friends;
using GuessWhoServerDomain.Domain.Parameters.Friends;
using GuessWhoServerDomain.Domain.Results.Friends;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoServices.Coordinators
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

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public FriendshipManager(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public IList<UserProfileSearchRecord> GetFriends(long accountId)
        {
            return ExecuteService(
                LOG_CTX_GET_FRIENDS,
                () =>
                {
                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    long userId = ResolveUserIdFromAccountOrThrow(unitOfWork, accountId);

                    return unitOfWork.Friendships.GetFriends(userId) ?? new List<UserProfileSearchRecord>();
                });
        }

        public IList<FriendRequestRecord> GetPendingRequests(long accountId)
        {
            return ExecuteService(
                LOG_CTX_GET_PENDING,
                () =>
                {
                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    long userId = ResolveUserIdFromAccountOrThrow(unitOfWork, accountId);

                    return unitOfWork.Friendships.GetPendingRequests(userId) ?? new List<FriendRequestRecord>();
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
                        throw FaultsFactory.Create(FriendFaultKeys.CODE_INVALID_DISPLAY_NAME);
                    }

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
                    return unitOfWork.Friendships.SearchProfilesByDisplayName(trimmed) ?? new List<UserProfileSearchRecord>();
                });
        }

        public SendFriendRequestResult SendFriendRequest(long fromAccountId, long toUserId, DateTime nowUtc)
        {
            return ExecuteService(
                LOG_CTX_SEND,
                () =>
                {
                    ValidateSendFriendRequestIdsOrThrow(fromAccountId, toUserId);

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    long fromUserId = ResolveUserIdFromAccountOrThrow(unitOfWork, fromAccountId);

                    EnsureNotSelfOrThrow(fromUserId, toUserId);

                    if (unitOfWork.Friendships.AreAlreadyFriends(fromUserId, toUserId))
                    {
                        throw FaultsFactory.Create(FriendFaultKeys.CODE_ALREADY_FRIENDS);
                    }

                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    FriendRequestDataResult inverseAutoAccepted =
                        unitOfWork.Friendships.TryAcceptInversePending(fromUserId, toUserId, nowUtc);

                    SendFriendRequestResult mappedInverse = TryMap(inverseAutoAccepted);

                    if (mappedInverse != null)
                    {
                        unitOfWork.Flush();
                        transaction.Commit();
                        return mappedInverse;
                    }

                    FriendRequestDataResult existingPending =
                        unitOfWork.Friendships.TryReturnExistingPending(fromUserId, toUserId);

                    SendFriendRequestResult mappedExisting = TryMap(existingPending);

                    if (mappedExisting != null)
                    {
                        transaction.Commit();
                        return mappedExisting;
                    }

                    IFriendRequestIdProvider created =
                        unitOfWork.Friendships.CreateNewRequest(fromUserId, toUserId, nowUtc);

                    unitOfWork.Flush();

                    long createdId = created.FriendRequestId;

                    if (createdId < MIN_VALID_ID)
                    {
                        throw FaultsFactory.Create(FriendFaultKeys.CODE_REQUEST_ID_NOT_GENERATED);
                    }

                    transaction.Commit();

                    return SendFriendRequestResult.OkCreated(createdId);
                });
        }

        public bool AcceptFriendRequest(FriendRequestActionArgs args)
        {
            return ExecuteService(
                LOG_CTX_ACCEPT,
                () =>
                {
                    ValidateActionArgsOrThrow(args);

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    ResolveUserIdFromAccountOrThrow(unitOfWork, args.AccountId);

                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    FriendRequestDataResult result = unitOfWork.Friendships.AcceptFriendRequest(args);

                    EnsureActionSucceededOrThrow(result);

                    unitOfWork.Flush();
                    transaction.Commit();

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

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    ResolveUserIdFromAccountOrThrow(unitOfWork, args.AccountId);

                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    FriendRequestDataResult result = unitOfWork.Friendships.RejectFriendRequest(args);

                    EnsureActionSucceededOrThrow(result);

                    unitOfWork.Flush();
                    transaction.Commit();

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

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

                    ResolveUserIdFromAccountOrThrow(unitOfWork, args.AccountId);

                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    FriendRequestDataResult result = unitOfWork.Friendships.CancelFriendRequest(args);

                    EnsureActionSucceededOrThrow(result);

                    unitOfWork.Flush();
                    transaction.Commit();

                    return true;
                });
        }

        private static void ValidateSendFriendRequestIdsOrThrow(long fromAccountId, long toUserId)
        {
            if (fromAccountId < MIN_VALID_ID || toUserId < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(FriendFaultKeys.CODE_INVALID_IDS);
            }
        }

        private long ResolveUserIdFromAccountOrThrow(IGuessWhoUnitOfWork unitOfWork, long accountId)
        {
            long userId = unitOfWork.Friendships.TryResolveUserIdFromAccountId(accountId);

            if (userId < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(FriendFaultKeys.CODE_NOT_AUTHORIZED);
            }

            return userId;
        }

        private static void EnsureNotSelfOrThrow(long fromUserId, long toUserId)
        {
            if (fromUserId == toUserId)
            {
                throw FaultsFactory.Create(FriendFaultKeys.CODE_CANNOT_FRIEND_SELF);
            }
        }

        private void EnsureDestinationActiveOrThrow(IGuessWhoUnitOfWork unitOfWork, long toUserId)
        {
            if (!unitOfWork.Friendships.IsUserProfileActive(toUserId))
            {
                throw FaultsFactory.Create(FriendFaultKeys.CODE_DESTINATION_INACTIVE);
            }
        }

        private static SendFriendRequestResult TryMap(FriendRequestDataResult dataResult)
        {
            if (dataResult == null || dataResult.Status == FriendRequestDataStatus.NotFound)
            {
                return null;
            }

            switch (dataResult.Status)
            {
                case FriendRequestDataStatus.AutoAccepted:
                    return SendFriendRequestResult.OkAutoAccepted();
                case FriendRequestDataStatus.ExistingPending:
                    return SendFriendRequestResult.ExistingPending(dataResult.FriendRequestId);
                case FriendRequestDataStatus.Created:
                    return SendFriendRequestResult.OkCreated(dataResult.FriendRequestId);
                default:
                    return null;
            }
        }

        private static void ValidateActionArgsOrThrow(FriendRequestActionArgs args)
        {
            if (args == null || args.AccountId < MIN_VALID_ID || args.FriendRequestId < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(FriendFaultKeys.CODE_INVALID_IDS);
            }
        }

        private static void EnsureActionSucceededOrThrow(FriendRequestDataResult result)
        {
            if (result == null) throw FaultsFactory.Create(FriendFaultKeys.CODE_NOT_FOUND);

            switch (result.Status)
            {
                case FriendRequestDataStatus.Accepted:
                case FriendRequestDataStatus.Rejected:
                case FriendRequestDataStatus.Canceled:
                    return;
                case FriendRequestDataStatus.NotFound:
                    throw FaultsFactory.Create(FriendFaultKeys.CODE_NOT_FOUND);
                case FriendRequestDataStatus.NotPending:
                    throw FaultsFactory.Create(FriendFaultKeys.CODE_NOT_PENDING);
                case FriendRequestDataStatus.NotAuthorized:
                    throw FaultsFactory.Create(FriendFaultKeys.CODE_NOT_AUTHORIZED);
                default:
                    throw FaultsFactory.Create(FriendFaultKeys.CODE_UNEXPECTED_ERROR);
            }
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}