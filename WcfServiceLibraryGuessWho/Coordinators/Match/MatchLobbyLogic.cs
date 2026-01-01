using ClassLibraryGuessWho.Data.Factories;
using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Enums.Matches.Outcomes;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Match.Outcomes;
using GuessWhoServices.Infrastructure;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoServices.Services.MatchApplication
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

        private readonly IGuessWhoUnitOfWorkFactory _unitOfWorkFactory;
        private readonly ILobbySubscriptionStore _subscriptionStore;
        private readonly IMatchCallbackDispatcher _callbackDispatcher;

        public MatchLobbyLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            ILobbySubscriptionStore subscriptionStore,
            IMatchCallbackDispatcher callbackDispatcher)
        {
            _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            _subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
            _callbackDispatcher = callbackDispatcher ?? throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public JoinMatchOutcome JoinMatch(JoinMatchRequest request)
        {
            if (request == null)
            {
                return JoinMatchOutcome.Fail(JoinMatchOutcomeCode.InvalidRequest);
            }

            string matchCode = NormalizeMatchCode(request.MatchCode);
            long userId = request.UserId;

            if (string.IsNullOrWhiteSpace(matchCode) || userId <= INVALID_ID)
            {
                return JoinMatchOutcome.Fail(JoinMatchOutcomeCode.InvalidInputs);
            }

            using IGuessWhoUnitOfWork unitOfWork = _unitOfWorkFactory.Create();
            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            IMatchRepository matchRepository = unitOfWork.Matches;

            MatchSnapshot openMatch = matchRepository.GetOpenMatchByCode(matchCode);

            if (!openMatch.IsValid)
            {
                transaction.Rollback();
                return JoinMatchOutcome.Fail(JoinMatchOutcomeCode.MatchNotFound);
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
                return JoinMatchOutcome.Fail(MapJoinCode(joinResult.Code));
            }

            unitOfWork.Flush();
            transaction.Commit();

            List<LobbyPlayerDto> players = MapPlayers(matchRepository.GetMatchPlayers(openMatch.MatchId)).ToList();

            LobbyPlayerDto hostPlayer = players.FirstOrDefault(player => player != null && player.IsHost);

            if (hostPlayer == null)
            {
                Logger.ErrorFormat("{0}: host not found. MatchId={1}.", CONTEXT_JOIN, openMatch.MatchId);
                return JoinMatchOutcome.Fail(JoinMatchOutcomeCode.HostNotFound);
            }

            LobbyPlayerDto joinedPlayer = players.FirstOrDefault(player => player != null && player.UserId == userId);

            if (joinedPlayer != null)
            {
                _callbackDispatcher.Broadcast(openMatch.MatchId, cb => cb.OnPlayerJoined(joinedPlayer));
            }

            var response = new JoinMatchResponse
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

            return JoinMatchOutcome.Success(response);
        }

        public LeaveMatchOutcome LeaveMatch(LeaveMatchRequest request)
        {
            if (request == null)
            {
                return LeaveMatchOutcome.Fail(LeaveMatchOutcomeCode.InvalidRequest);
            }

            long matchId = request.MatchId;
            long userId = request.UserId;

            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                return LeaveMatchOutcome.Fail(LeaveMatchOutcomeCode.InvalidInputs);
            }

            using IGuessWhoUnitOfWork unitOfWork = _unitOfWorkFactory.Create();
            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            var leaveArgs = new MatchPlayerArgs
            {
                MatchId = matchId,
                UserProfileId = userId
            };

            LeaveMatchResult leaveResult = unitOfWork.Matches.LeaveMatch(leaveArgs);

            if (!leaveResult.IsSuccess)
            {
                transaction.Rollback();
                return LeaveMatchOutcome.Fail(MapLeaveCode(leaveResult.Code));
            }

            unitOfWork.Flush();
            transaction.Commit();

            _callbackDispatcher.Broadcast(matchId, cb => cb.OnPlayerLeft(new LobbyPlayerDto
            {
                MatchId = matchId,
                UserId = userId
            }));

            return LeaveMatchOutcome.Success();
        }

        public SetReadyOutcome SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            if (request == null)
            {
                return SetReadyOutcome.Fail(SetReadyOutcomeCode.InvalidRequest);
            }

            long matchId = request.MatchId;
            long userId = request.UserId;

            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                return SetReadyOutcome.Fail(SetReadyOutcomeCode.InvalidInputs);
            }

            using IGuessWhoUnitOfWork unitOfWork = _unitOfWorkFactory.Create();
            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            var markReadyArgs = new MatchPlayerArgs
            {
                MatchId = matchId,
                UserProfileId = userId
            };

            MarkReadyResult readyResult = unitOfWork.Matches.MarkReady(markReadyArgs);

            if (!readyResult.IsSuccess)
            {
                transaction.Rollback();
                return SetReadyOutcome.Fail(MapReadyCode(readyResult.Code));
            }

            unitOfWork.Flush();
            transaction.Commit();

            _callbackDispatcher.Broadcast(matchId, cb => cb.OnReadyChanged(new LobbyPlayerDto
            {
                MatchId = matchId,
                UserId = userId,
                IsReady = true
            }));

            return SetReadyOutcome.Success();
        }

        public void SubscribeLobby(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= INVALID_ID || callbackChannel == null)
            {
                Logger.WarnFormat("{0}: invalid inputs. MatchId={1}, HasCallback={2}.",
                    CONTEXT_SUBSCRIBE, matchId, callbackChannel != null);
                return;
            }

            _subscriptionStore.Subscribe(matchId, callbackChannel);
        }

        public void UnsubscribeLobby(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= INVALID_ID || callbackChannel == null)
            {
                Logger.WarnFormat("{0}: invalid inputs. MatchId={1}, HasCallback={2}.",
                    CONTEXT_UNSUBSCRIBE, matchId, callbackChannel != null);
                return;
            }

            _subscriptionStore.Unsubscribe(matchId, callbackChannel);
        }

        private static string NormalizeMatchCode(string matchCode)
        {
            return (matchCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static JoinMatchOutcomeCode MapJoinCode(JoinMatchResultCode code)
        {
            switch (code)
            {
                case JoinMatchResultCode.MatchNotFound:
                    return JoinMatchOutcomeCode.MatchNotFound;

                case JoinMatchResultCode.MatchNotJoinable:
                    return JoinMatchOutcomeCode.MatchNotJoinable;

                case JoinMatchResultCode.GuestSlotTaken:
                    return JoinMatchOutcomeCode.MatchFull;

                case JoinMatchResultCode.PlayerAlreadyInMatch:
                    return JoinMatchOutcomeCode.PlayerAlreadyInMatch;

                case JoinMatchResultCode.InOtherActiveMatch:
                    return JoinMatchOutcomeCode.InOtherActiveMatch;

                default:
                    return JoinMatchOutcomeCode.OperationConflict;
            }
        }

        private static LeaveMatchOutcomeCode MapLeaveCode(LeaveMatchResultCode code)
        {
            switch (code)
            {
                case LeaveMatchResultCode.MatchNotFound:
                    return LeaveMatchOutcomeCode.MatchNotFound;

                case LeaveMatchResultCode.PlayerNotInMatch:
                    return LeaveMatchOutcomeCode.PlayerNotInMatch;

                case LeaveMatchResultCode.PlayerAlreadyLeft:
                    return LeaveMatchOutcomeCode.PlayerAlreadyLeft;

                default:
                    return LeaveMatchOutcomeCode.OperationConflict;
            }
        }

        private static SetReadyOutcomeCode MapReadyCode(MarkReadyResultCode code)
        {
            switch (code)
            {
                case MarkReadyResultCode.PlayerNotFound:
                    return SetReadyOutcomeCode.PlayerNotInMatch;

                case MarkReadyResultCode.PlayerAlreadyLeft:
                    return SetReadyOutcomeCode.PlayerAlreadyLeft;

                case MarkReadyResultCode.MatchNotInLobby:
                    return SetReadyOutcomeCode.MatchNotInLobby;

                default:
                    return SetReadyOutcomeCode.OperationConflict;
            }
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
