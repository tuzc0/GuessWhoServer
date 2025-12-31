using ClassLibraryGuessWho.Data.Factories;
using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace WcfServiceLibraryGuessWho.Coordinators.Match
{
    public sealed class MatchLobbyLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchLobbyLogic));

        private const string CONTEXT_JOIN = nameof(MatchLobbyLogic) + "." + nameof(JoinMatch);
        private const string CONTEXT_LEAVE = nameof(MatchLobbyLogic) + "." + nameof(LeaveMatch);
        private const string CONTEXT_READY = nameof(MatchLobbyLogic) + "." + nameof(SetPlayerReadyStatus);
        private const string CONTEXT_SUBSCRIBE = nameof(MatchLobbyLogic) + "." + nameof(SubscribeLobby);
        private const string CONTEXT_UNSUBSCRIBE = nameof(MatchLobbyLogic) + "." + nameof(UnsubscribeLobby);

        private const long INVALID_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly ILobbySubscriptionStore subscriptionStore;
        private readonly IMatchCallbackDispatcher callbackDispatcher;

        public MatchLobbyLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            ILobbySubscriptionStore subscriptionStore,
            IMatchCallbackDispatcher callbackDispatcher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.subscriptionStore = subscriptionStore ??
                throw new ArgumentNullException(nameof(subscriptionStore));
            this.callbackDispatcher = callbackDispatcher ??
                throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            EnsureRequestNotNull(request);

            string matchCode = NormalizeMatchCode(request.MatchCode);
            long userId = request.UserId;

            EnsureJoinInputs(matchCode, userId);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            IMatchRepository matchRepository = unitOfWork.Matches;

            MatchSnapshot openMatch = matchRepository.GetOpenMatchByCode(matchCode);

            if (!openMatch.IsValid)
            {
                transaction.Rollback();

                throw CreateMatchFault(
                    MatchFaultKeys.CODE_MATCH_NOT_FOUND,
                    MatchFaultKeys.MSG_MATCH_NOT_FOUND,
                    MatchFaultKeys.FALLBACK_MATCH_NOT_FOUND);
            }

            var joinArgs = new JoinMatchArgs
            {
                MatchId = openMatch.MatchId,
                MatchCode = matchCode,
                UserProfileId = userId
            };

            JoinMatchResult joinResult = matchRepository.AddPlayerToMatchByCode(joinArgs);

            if (!joinResult.IsValid)
            {
                transaction.Rollback();
                ThrowJoinFault(joinResult.Code, openMatch.MatchId, userId);
            }

            unitOfWork.Flush();
            transaction.Commit();

            List<LobbyPlayerDto> players = MapPlayers(matchRepository.GetMatchPlayers(openMatch.MatchId)).ToList();

            LobbyPlayerDto hostPlayer = players.FirstOrDefault(player => player != null && player.IsHost);

            if (hostPlayer == null)
            {
                Logger.ErrorFormat("{0}: host not found. MatchId={1}", CONTEXT_JOIN, openMatch.MatchId);
                throw CreateInfrastructureUnexpectedFault();
            }

            LobbyPlayerDto joinedPlayer = players.FirstOrDefault(player => player != null && player.UserId == userId);

            if (joinedPlayer != null)
            {
                callbackDispatcher.Broadcast(openMatch.MatchId, cb => cb.OnPlayerJoined(joinedPlayer));
            }

            return new JoinMatchResponse
            {
                MatchId = openMatch.MatchId,
                Code = openMatch.MatchCode ?? string.Empty,
                StatusId = openMatch.StatusId,
                Mode = openMatch.ModeId,
                Visibility = openMatch.VisibilityId,
                CreateAtUtc = openMatch.CreatedAtUtc,
                HostUserId = hostPlayer.UserId,
                Players = players
            };
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            EnsureRequestNotNull(request);

            long matchId = request.MatchId;
            long userId = request.UserId;

            EnsureMatchAndUser(matchId, userId);

            using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                var leaveArgs = new MatchPlayerArgs
                {
                    MatchId = matchId,
                    UserProfileId = userId
                };

                LeaveMatchResult leaveResult = unitOfWork.Matches.LeaveMatch(leaveArgs);

                if (!leaveResult.IsSuccess)
                {
                    transaction.Rollback();
                    ThrowLeaveFault(leaveResult.Code, matchId, userId);
                }

                unitOfWork.Flush();
                transaction.Commit();
            }

            callbackDispatcher.Broadcast(matchId, cb => cb.OnPlayerLeft(new LobbyPlayerDto
            {
                MatchId = matchId,
                UserId = userId
            }));

            return new BasicResponse { Success = true };
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            EnsureRequestNotNull(request);

            long matchId = request.MatchId;
            long userId = request.UserId;

            EnsureMatchAndUser(matchId, userId);

            using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                var markReadyArgs = new MatchPlayerArgs
                {
                    MatchId = matchId,
                    UserProfileId = userId
                };

                MarkReadyResult readyResult = unitOfWork.Matches.MarkReady(markReadyArgs);

                if (!readyResult.IsSuccess)
                {
                    transaction.Rollback();
                    ThrowReadyFault(readyResult.Code, matchId, userId);
                }

                unitOfWork.Flush();
                transaction.Commit();
            }

            callbackDispatcher.Broadcast(matchId, cb => cb.OnReadyChanged(new LobbyPlayerDto
            {
                MatchId = matchId,
                UserId = userId,
                IsReady = true
            }));

            return new BasicResponse { Success = true };
        }

        public void SubscribeLobby(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= INVALID_ID || callbackChannel == null)
            {
                Logger.WarnFormat("{0}: invalid inputs. MatchId={1}, HasCallback={2}",
                    CONTEXT_SUBSCRIBE, matchId, callbackChannel != null);
                return;
            }

            subscriptionStore.Subscribe(matchId, callbackChannel);
        }

        public void UnsubscribeLobby(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= INVALID_ID || callbackChannel == null)
            {
                Logger.WarnFormat("{0}: invalid inputs. MatchId={1}, HasCallback={2}", 
                    CONTEXT_UNSUBSCRIBE, matchId, callbackChannel != null);
                return;
            }

            subscriptionStore.Unsubscribe(matchId, callbackChannel);
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                InfrastructureFaultKeys.CODE_REQUEST_NULL,
                InfrastructureFaultKeys.MSG_REQUEST_NULL,
                InfrastructureFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static string NormalizeMatchCode(string matchCode)
        {
            return (matchCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static void EnsureJoinInputs(string matchCode, long userId)
        {
            if (!string.IsNullOrWhiteSpace(matchCode) && userId > INVALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(
                MatchFaultKeys.CODE_MATCH_NOT_JOINABLE,
                MatchFaultKeys.MSG_MATCH_NOT_JOINABLE,
                MatchFaultKeys.FALLBACK_MATCH_NOT_JOINABLE);
        }

        private static void EnsureMatchAndUser(long matchId, long userId)
        {
            if (matchId > INVALID_ID && userId > INVALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(
                MatchFaultKeys.CODE_PLAYER_NOT_IN_MATCH,
                MatchFaultKeys.MSG_PLAYER_NOT_IN_MATCH,
                MatchFaultKeys.FALLBACK_PLAYER_NOT_IN_MATCH);
        }

        private void ThrowJoinFault(JoinMatchResultCode code, long matchId, long userId)
        {
            switch (code)
            {
                case JoinMatchResultCode.MatchNotFound:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_MATCH_NOT_FOUND, 
                        MatchFaultKeys.MSG_MATCH_NOT_FOUND, 
                        MatchFaultKeys.FALLBACK_MATCH_NOT_FOUND);

                case JoinMatchResultCode.MatchNotJoinable:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_MATCH_NOT_JOINABLE, 
                        MatchFaultKeys.MSG_MATCH_NOT_JOINABLE, 
                        MatchFaultKeys.FALLBACK_MATCH_NOT_JOINABLE);

                case JoinMatchResultCode.GuestSlotTaken:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_MATCH_FULL, 
                        MatchFaultKeys.MSG_MATCH_FULL, 
                        MatchFaultKeys.FALLBACK_MATCH_FULL);

                case JoinMatchResultCode.PlayerAlreadyInMatch:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_PLAYER_ALREADY_IN_MATCH, 
                        MatchFaultKeys.MSG_PLAYER_ALREADY_IN_MATCH, 
                        MatchFaultKeys.FALLBACK_PLAYER_ALREADY_IN_MATCH);

                case JoinMatchResultCode.InOtherActiveMatch:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_IN_OTHER_ACTIVE_MATCH, 
                        MatchFaultKeys.MSG_IN_OTHER_ACTIVE_MATCH, 
                        MatchFaultKeys.FALLBACK_IN_OTHER_ACTIVE_MATCH);

                default:
                    
                    Logger.ErrorFormat("{0}: unexpected JoinMatchResultCode. MatchId={1}, UserId={2}, Code={3}",
                        CONTEXT_JOIN, matchId, userId, code);
                    throw CreateInfrastructureUnexpectedFault();
            }
        }

        private void ThrowLeaveFault(LeaveMatchResultCode code, long matchId, long userId)
        {
            switch (code)
            {
                case LeaveMatchResultCode.MatchNotFound:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_MATCH_NOT_FOUND, 
                        MatchFaultKeys.MSG_MATCH_NOT_FOUND, 
                        MatchFaultKeys.FALLBACK_MATCH_NOT_FOUND);

                case LeaveMatchResultCode.PlayerNotInMatch:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_PLAYER_NOT_IN_MATCH, 
                        MatchFaultKeys.MSG_PLAYER_NOT_IN_MATCH, 
                        MatchFaultKeys.FALLBACK_PLAYER_NOT_IN_MATCH);

                case LeaveMatchResultCode.PlayerAlreadyLeft:

                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_PLAYER_ALREADY_LEFT, 
                        MatchFaultKeys.MSG_PLAYER_ALREADY_LEFT, 
                        MatchFaultKeys.FALLBACK_PLAYER_ALREADY_LEFT);

                default:
                    Logger.ErrorFormat("{0}: unexpected LeaveMatchResultCode. MatchId={1}, UserId={2}, Code={3}",
                        CONTEXT_LEAVE, matchId, userId, code);
                    throw CreateInfrastructureUnexpectedFault();
            }
        }

        private void ThrowReadyFault(MarkReadyResultCode code, long matchId, long userId)
        {
            switch (code)
            {
                case MarkReadyResultCode.PlayerNotFound:

                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_PLAYER_NOT_IN_MATCH, 
                        MatchFaultKeys.MSG_PLAYER_NOT_IN_MATCH, 
                        MatchFaultKeys.FALLBACK_PLAYER_NOT_IN_MATCH);

                case MarkReadyResultCode.PlayerAlreadyLeft:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_PLAYER_ALREADY_LEFT, 
                        MatchFaultKeys.MSG_PLAYER_ALREADY_LEFT, 
                        MatchFaultKeys.FALLBACK_PLAYER_ALREADY_LEFT);

                case MarkReadyResultCode.MatchNotInLobby:
                    
                    throw CreateMatchFault(
                        MatchFaultKeys.CODE_MATCH_NOT_IN_LOBBY, 
                        MatchFaultKeys.MSG_MATCH_NOT_IN_LOBBY, 
                        MatchFaultKeys.FALLBACK_MATCH_NOT_IN_LOBBY);

                default:
                    
                    Logger.ErrorFormat("{0}: unexpected MarkReadyResultCode. MatchId={1}, UserId={2}, Code={3}", 
                        CONTEXT_READY, matchId, userId, code);
                    throw CreateInfrastructureUnexpectedFault();
            }
        }

        private static FaultException<ServiceFault> CreateMatchFault(string code, string messageKey, string fallbackMessage)
        {
            return FaultsFactory.Create(code, messageKey, fallbackMessage);
        }

        private static FaultException<ServiceFault> CreateInfrastructureUnexpectedFault()
        {
            return FaultsFactory.Create(
                InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
        }

        private static IReadOnlyList<LobbyPlayerDto> MapPlayers(IReadOnlyList<LobbyPlayerSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return Array.Empty<LobbyPlayerDto>();
            }

            return snapshots
                .Where(snapshot => snapshot != null)
                .Select(snapshot => new LobbyPlayerDto
                {
                    MatchId = snapshot.MatchId,
                    UserId = snapshot.UserId,
                    DisplayName = snapshot.DisplayName ?? string.Empty,
                    AvatarId = snapshot.AvatarId ?? string.Empty,
                    SlotNumber = snapshot.SlotNumber,
                    IsReady = snapshot.IsReady,
                    IsHost = snapshot.IsHost
                })
                .ToList();
        }
    }
}
