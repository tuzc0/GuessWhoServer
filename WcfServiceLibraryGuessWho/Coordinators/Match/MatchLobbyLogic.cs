using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Coordinators.InternalDtos;
using log4net;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.Match
{
    public sealed class MatchLobbyLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchLobbyLogic));

        private const string CONTEXT_JOIN = nameof(MatchLobbyLogic) + "." + nameof(JoinMatch);
        private const string CONTEXT_SUBSCRIBE = nameof(MatchLobbyLogic) + "." + nameof(SubscribeLobby);
        private const string CONTEXT_UNSUBSCRIBE = nameof(MatchLobbyLogic) + "." + nameof(UnsubscribeLobby);

        private const long INVALID_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory;
        private readonly ILobbySubscriptionOperations lobbySubscriptionOperations;

        public MatchLobbyLogic(
            IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory,
            ILobbySubscriptionOperations lobbySubscriptionOperations)
        {
            this.guessWhoUnitOfWorkFactory = guessWhoUnitOfWorkFactory ??
                throw new ArgumentNullException(nameof(guessWhoUnitOfWorkFactory));

            this.lobbySubscriptionOperations = lobbySubscriptionOperations ??
                throw new ArgumentNullException(nameof(lobbySubscriptionOperations));
        }

        public JoinLobbyLogicResult JoinMatch(JoinLobbyArgs joinLobbyArgs)
        {
            JoinInput joinInput = BuildJoinInputOrInvalid(joinLobbyArgs);

            if (!joinInput.IsValid)
            {
                return CreateJoinFailure(JoinMatchResultCode.InvalidArgs);
            }

            using IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create();
            IMatchRepository matchRepository = unitOfWork.Matches;

            MatchSnapshot openLobbyMatch = matchRepository.GetOpenMatchByCode(joinInput.MatchCode);

            if (!openLobbyMatch.IsValid)
            {
                return CreateJoinFailure(JoinMatchResultCode.MatchNotFound);
            }

            var joinMatchArgs = new JoinMatchArgs
            {
                MatchId = openLobbyMatch.MatchId,
                MatchCode = joinInput.MatchCode,
                UserProfileId = joinInput.UserId
            };

            JoinMatchResult joinMatchResult;

            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                joinMatchResult = ExecuteJoinMatch(matchRepository, joinMatchArgs);

                if (!joinMatchResult.IsValid)
                {
                    transaction.Rollback();
                    return CreateJoinDbFailure(joinMatchResult);
                }

                unitOfWork.Flush();
                transaction.Commit();
            }

            IReadOnlyList<LobbyPlayerSnapshot> lobbyPlayers =
                matchRepository.GetMatchPlayers(openLobbyMatch.MatchId) ?? Array.Empty<LobbyPlayerSnapshot>();

            ResolvedLobbyPlayers resolvedPlayers = ResolveHostAndJoined(lobbyPlayers, joinInput.UserId);

            if (!resolvedPlayers.HasHostPlayer)
            {
                Logger.ErrorFormat("{0}: host not found. MatchId={1}.", CONTEXT_JOIN, openLobbyMatch.MatchId);

                return CreateJoinHostNotFoundFailure(openLobbyMatch.MatchId, lobbyPlayers, resolvedPlayers);
            }

            return new JoinLobbyLogicResult(
                result: joinMatchResult,
                match: openLobbyMatch,
                players: lobbyPlayers,
                hasHostPlayer: resolvedPlayers.HasHostPlayer,
                hostPlayer: resolvedPlayers.HostPlayer,
                hasJoinedPlayer: resolvedPlayers.HasJoinedPlayer,
                joinedPlayer: resolvedPlayers.JoinedPlayer);
        }

        public LeaveLobbyLogicResult LeaveMatch(long matchId, long userId)
        {
            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                return new LeaveLobbyLogicResult(
                    result: LeaveMatchResult.Fail(LeaveMatchResultCode.PlayerNotInMatch),
                    wasHost: false,
                    playersAfter: Array.Empty<LobbyPlayerSnapshot>());
            }

            using IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create();
            IMatchRepository matchRepository = unitOfWork.Matches;

            IReadOnlyList<LobbyPlayerSnapshot> playersBeforeLeave =
                matchRepository.GetMatchPlayers(matchId) ?? Array.Empty<LobbyPlayerSnapshot>();

            bool wasHost = ResolveWasHost(playersBeforeLeave, userId);

            var matchPlayerArgs = new MatchPlayerArgs
            {
                MatchId = matchId,
                UserProfileId = userId
            };

            LeaveMatchResult leaveMatchResult;

            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                leaveMatchResult = ExecuteLeaveMatch(matchRepository, matchPlayerArgs);

                if (!leaveMatchResult.IsSuccess)
                {
                    transaction.Rollback();

                    return new LeaveLobbyLogicResult(
                        result: leaveMatchResult,
                        wasHost: wasHost,
                        playersAfter: Array.Empty<LobbyPlayerSnapshot>());
                }

                unitOfWork.Flush();
                transaction.Commit();
            }

            IReadOnlyList<LobbyPlayerSnapshot> playersAfterLeave =
                matchRepository.GetMatchPlayers(matchId) ?? Array.Empty<LobbyPlayerSnapshot>();

            return new LeaveLobbyLogicResult(
                result: leaveMatchResult,
                wasHost: wasHost,
                playersAfter: playersAfterLeave);
        }

        public ReadyLobbyLogicResult SetPlayerReadyStatus(long matchId, long userId)
        {
            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                return new ReadyLobbyLogicResult(
                    result: MarkReadyResult.Fail(MarkReadyResultCode.PlayerNotFound),
                    playersAfter: Array.Empty<LobbyPlayerSnapshot>(),
                    hasReadyPlayer: false,
                    readyPlayer: default);
            }

            using IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create();
            IMatchRepository matchRepository = unitOfWork.Matches;

            var matchPlayerArgs = new MatchPlayerArgs
            {
                MatchId = matchId,
                UserProfileId = userId
            };

            MarkReadyResult markReadyResult;

            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                markReadyResult = ExecuteMarkReady(matchRepository, matchPlayerArgs);

                if (!markReadyResult.IsSuccess)
                {
                    transaction.Rollback();

                    return new ReadyLobbyLogicResult(
                        result: markReadyResult,
                        playersAfter: Array.Empty<LobbyPlayerSnapshot>(),
                        hasReadyPlayer: false,
                        readyPlayer: default);
                }

                unitOfWork.Flush();
                transaction.Commit();
            }

            IReadOnlyList<LobbyPlayerSnapshot> playersAfterReady =
                matchRepository.GetMatchPlayers(matchId) ?? Array.Empty<LobbyPlayerSnapshot>();

            ResolvedReadyPlayer resolvedReadyPlayer = ResolveReadyPlayer(playersAfterReady, userId);

            return new ReadyLobbyLogicResult(
                result: markReadyResult,
                playersAfter: playersAfterReady,
                hasReadyPlayer: resolvedReadyPlayer.HasReadyPlayer,
                readyPlayer: resolvedReadyPlayer.ReadyPlayer);
        }

        public bool SubscribeLobby(LobbySubscriptionArgs lobbySubscriptionArgs)
        {
            if (lobbySubscriptionArgs == null ||
                lobbySubscriptionArgs.MatchId <= INVALID_ID ||
                lobbySubscriptionArgs.UserId <= INVALID_ID)
            {
                long matchId = lobbySubscriptionArgs == null ? INVALID_ID : lobbySubscriptionArgs.MatchId;
                long userId = lobbySubscriptionArgs == null ? INVALID_ID : lobbySubscriptionArgs.UserId;

                Logger.WarnFormat("{0}: invalid inputs. MatchId={1}, UserId={2}.",
                    CONTEXT_SUBSCRIBE,
                    lobbySubscriptionArgs == null ? INVALID_ID : lobbySubscriptionArgs.MatchId,
                    lobbySubscriptionArgs == null ? INVALID_ID : lobbySubscriptionArgs.UserId);

                return false;
            }

            return lobbySubscriptionOperations.Subscribe(lobbySubscriptionArgs);
        }

        public bool UnsubscribeLobby(LobbySubscriptionArgs lobbySubscriptionArgs)
        {
            if (lobbySubscriptionArgs == null ||
                lobbySubscriptionArgs.MatchId <= INVALID_ID ||
                lobbySubscriptionArgs.UserId <= INVALID_ID)
            {
                Logger.WarnFormat("{0}: invalid inputs. MatchId={1}, UserId={2}.",
                    CONTEXT_UNSUBSCRIBE,
                    lobbySubscriptionArgs == null ? INVALID_ID : lobbySubscriptionArgs.MatchId,
                    lobbySubscriptionArgs == null ? INVALID_ID : lobbySubscriptionArgs.UserId);

                return false;
            }

            return lobbySubscriptionOperations.Unsubscribe(lobbySubscriptionArgs);
        }

        private static JoinInput BuildJoinInputOrInvalid(JoinLobbyArgs joinLobbyArgs)
        {
            if (joinLobbyArgs == null)
            {
                return JoinInput.Invalid();
            }

            long userId = joinLobbyArgs.UserId;
            string matchCode = NormalizeMatchCode(joinLobbyArgs.MatchCode);

            if (userId <= INVALID_ID || string.IsNullOrWhiteSpace(matchCode))
            {
                return JoinInput.Invalid();
            }

            return JoinInput.Valid(userId, matchCode);
        }

        private static JoinMatchResult ExecuteJoinMatch(IMatchRepository matchRepository, JoinMatchArgs joinMatchArgs)
        {
            return matchRepository.AddPlayerToMatchByCode(joinMatchArgs);
        }

        private static LeaveMatchResult ExecuteLeaveMatch(IMatchRepository matchRepository, MatchPlayerArgs matchPlayerArgs)
        {
            return matchRepository.LeaveMatch(matchPlayerArgs);
        }

        private static MarkReadyResult ExecuteMarkReady(IMatchRepository matchRepository, MatchPlayerArgs matchPlayerArgs)
        {
            return matchRepository.MarkReady(matchPlayerArgs);
        }

        private static ResolvedLobbyPlayers ResolveHostAndJoined(IReadOnlyList<LobbyPlayerSnapshot> lobbyPlayers, long userId)
        {
            if (lobbyPlayers == null || lobbyPlayers.Count == 0 || userId <= INVALID_ID)
            {
                return ResolvedLobbyPlayers.Empty();
            }

            bool hasHostPlayer = false;
            LobbyPlayerSnapshot hostPlayer = default;

            bool hasJoinedPlayer = false;
            LobbyPlayerSnapshot joinedPlayer = default;

            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                LobbyPlayerSnapshot player = lobbyPlayers[i];

                if (!hasHostPlayer && player.IsHost && player.UserId > INVALID_ID)
                {
                    hostPlayer = player;
                    hasHostPlayer = true;
                }

                if (!hasJoinedPlayer && player.UserId == userId)
                {
                    joinedPlayer = player;
                    hasJoinedPlayer = true;
                }

                if (hasHostPlayer && hasJoinedPlayer)
                {
                    break;
                }
            }

            return new ResolvedLobbyPlayers(hasHostPlayer, hostPlayer, hasJoinedPlayer, joinedPlayer);
        }

        private static bool ResolveWasHost(IReadOnlyList<LobbyPlayerSnapshot> lobbyPlayers, long userId)
        {
            if (lobbyPlayers == null || lobbyPlayers.Count == 0 || userId <= INVALID_ID)
            {
                return false;
            }

            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                LobbyPlayerSnapshot player = lobbyPlayers[i];

                if (player.UserId == userId)
                {
                    return player.IsHost;
                }
            }

            return false;
        }

        private static ResolvedReadyPlayer ResolveReadyPlayer(IReadOnlyList<LobbyPlayerSnapshot> lobbyPlayers, long userId)
        {
            if (lobbyPlayers == null || lobbyPlayers.Count == 0 || userId <= INVALID_ID)
            {
                return ResolvedReadyPlayer.Empty();
            }

            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                LobbyPlayerSnapshot player = lobbyPlayers[i];

                if (player.UserId == userId)
                {
                    return new ResolvedReadyPlayer(true, player);
                }
            }

            return ResolvedReadyPlayer.Empty();
        }

        private static string NormalizeMatchCode(string matchCode)
        {
            return (matchCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static JoinLobbyLogicResult CreateJoinFailure(JoinMatchResultCode joinMatchResultCode)
        {
            return new JoinLobbyLogicResult(
                result: JoinMatchResult.Fail(joinMatchResultCode, INVALID_ID),
                match: MatchSnapshot.CreateInvalid(),
                players: Array.Empty<LobbyPlayerSnapshot>(),
                hasHostPlayer: false,
                hostPlayer: default,
                hasJoinedPlayer: false,
                joinedPlayer: default);
        }

        private static JoinLobbyLogicResult CreateJoinDbFailure(JoinMatchResult joinMatchResult)
        {
            return new JoinLobbyLogicResult(
                result: joinMatchResult,
                match: MatchSnapshot.CreateInvalid(),
                players: Array.Empty<LobbyPlayerSnapshot>(),
                hasHostPlayer: false,
                hostPlayer: default,
                hasJoinedPlayer: false,
                joinedPlayer: default);
        }

        private static JoinLobbyLogicResult CreateJoinHostNotFoundFailure(
            long matchId,
            IReadOnlyList<LobbyPlayerSnapshot> lobbyPlayers,
            ResolvedLobbyPlayers resolvedPlayers)
        {
            IReadOnlyList<LobbyPlayerSnapshot> safePlayers = lobbyPlayers ?? Array.Empty<LobbyPlayerSnapshot>();

            return new JoinLobbyLogicResult(
                result: JoinMatchResult.Fail(JoinMatchResultCode.OperationConflict, matchId),
                match: MatchSnapshot.CreateInvalid(),
                players: safePlayers,
                hasHostPlayer: false,
                hostPlayer: default,
                hasJoinedPlayer: resolvedPlayers.HasJoinedPlayer,
                joinedPlayer: resolvedPlayers.JoinedPlayer);
        }

        private readonly struct JoinInput
        {
            public readonly bool IsValid;
            public readonly long UserId;
            public readonly string MatchCode;

            private JoinInput(bool isValid, long userId, string matchCode)
            {
                IsValid = isValid;
                UserId = userId;
                MatchCode = matchCode ?? string.Empty;
            }

            public static JoinInput Invalid() => new JoinInput(false, INVALID_ID, string.Empty);

            public static JoinInput Valid(long userId, string matchCode) => new(true, userId, matchCode);
        }

        private readonly struct ResolvedLobbyPlayers
        {
            public readonly bool HasHostPlayer;
            public readonly LobbyPlayerSnapshot HostPlayer;
            public readonly bool HasJoinedPlayer;
            public readonly LobbyPlayerSnapshot JoinedPlayer;

            public ResolvedLobbyPlayers(
                bool hasHostPlayer,
                LobbyPlayerSnapshot hostPlayer,
                bool hasJoinedPlayer,
                LobbyPlayerSnapshot joinedPlayer)
            {
                HasHostPlayer = hasHostPlayer;
                HostPlayer = hostPlayer;
                HasJoinedPlayer = hasJoinedPlayer;
                JoinedPlayer = joinedPlayer;
            }

            public static ResolvedLobbyPlayers Empty() => new(false, default, false, default);
        }

        private readonly struct ResolvedReadyPlayer
        {
            public readonly bool HasReadyPlayer;
            public readonly LobbyPlayerSnapshot ReadyPlayer;

            public ResolvedReadyPlayer(bool hasReadyPlayer, LobbyPlayerSnapshot readyPlayer)
            {
                HasReadyPlayer = hasReadyPlayer;
                ReadyPlayer = readyPlayer;
            }

            public static ResolvedReadyPlayer Empty() => new ResolvedReadyPlayer(false, default);
        }
    }
}
