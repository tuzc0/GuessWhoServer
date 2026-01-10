using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Communication.Email.Builders.Context;
using GuessWhoCore.Contracts.Response;
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
        private const string CONTEXT_INVITATION = nameof(MatchLobbyLogic) + "." + nameof(SendInvitation);

        private const long INVALID_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory;
        private readonly ILobbySubscriptionOperations lobbySubscriptionOperations;
        private readonly IEmailSender emailSender;
        private readonly IEmailMessageBuilder<MatchInvitationEmailContext> invitationBuilder;

        public MatchLobbyLogic(
            IGuessWhoUnitOfWorkFactory guessWhoUnitOfWorkFactory,
            ILobbySubscriptionOperations lobbySubscriptionOperations,
            IEmailSender emailSender,
            IEmailMessageBuilder<MatchInvitationEmailContext> invitationBuilder)
        {
            this.guessWhoUnitOfWorkFactory = guessWhoUnitOfWorkFactory ??
                throw new ArgumentNullException(nameof(guessWhoUnitOfWorkFactory));

            this.lobbySubscriptionOperations = lobbySubscriptionOperations ??
                throw new ArgumentNullException(nameof(lobbySubscriptionOperations));

            this.emailSender = emailSender ??
                throw new ArgumentNullException(nameof(emailSender));

            this.invitationBuilder = invitationBuilder ??
                throw new ArgumentNullException(nameof(invitationBuilder));
        }

        public BasicResponse SendInvitation(long matchId, long inviterId, string targetEmail, long targetUserId = 0)
        {
            try
            {
                using IGuessWhoUnitOfWork unitOfWork = guessWhoUnitOfWorkFactory.Create();

                var matchSnapshot = unitOfWork.Matches.GetMatchById(matchId);
                var inviterSnapshot = unitOfWork.UserProfiles.GetUserProfileById(inviterId);

                if (!matchSnapshot.IsValid || !inviterSnapshot.IsValid)
                {
                    return new BasicResponse
                    {
                        Success = false,
                        Code = "DATA_NOT_FOUND",
                        MeesageKey = "Match.Invitation.DataNotFound"
                    };
                }

                if (string.IsNullOrWhiteSpace(targetEmail) && targetUserId > 0)
                {
                    var targetAccountResult = unitOfWork.UserAccounts.GetAccountWithProfileByUserId(targetUserId);

                    if (targetAccountResult != null && targetAccountResult.Account != null)
                    {
                        targetEmail = targetAccountResult.Account.Email;
                    }
                }

                if (string.IsNullOrWhiteSpace(targetEmail))
                {
                    return new BasicResponse
                    {
                        Success = false,
                        Code = "EMAIL_REQUIRED",
                        MeesageKey = "Match.Invitation.EmailRequired"
                    };
                }

                Guid invitationToken = Guid.NewGuid();
                DateTime expirationDate = DateTime.UtcNow.AddHours(24);

                using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
                {
                    bool added = unitOfWork.MatchInvitations.AddMatchInvitation(
                        matchId,
                        inviterId,
                        targetEmail,
                        invitationToken,
                        expirationDate,
                        targetUserId
                    );

                    if (!added)
                    {
                        transaction.Rollback();
                        return new BasicResponse { Success = false, Code = "DB_ERROR" };
                    }

                    unitOfWork.Flush();
                    transaction.Commit();
                }

                var emailContext = new MatchInvitationEmailContext(targetEmail, inviterSnapshot.DisplayName, matchSnapshot.MatchCode);
                var emailMessage = invitationBuilder.Build(emailContext);
                var emailResult = emailSender.Send(emailMessage);

                return new BasicResponse
                {
                    Success = emailResult.IsSuccess,
                    Code = emailResult.IsSuccess ? "OK" : emailResult.ErrorCode,
                    MeesageKey = emailResult.IsSuccess ? string.Empty : "Match.Invitation.EmailError"
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"{CONTEXT_INVITATION}: Unexpected error.", ex);
                return new BasicResponse
                {
                    Success = false,
                    Code = "INTERNAL_ERROR",
                    MeesageKey = "Match.Invitation.InternalError"
                };
            }
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

                if (joinMatchResult.Code == JoinMatchResultCode.PlayerAlreadyInMatch)
                {
                    transaction.Rollback();
                    joinMatchResult = JoinMatchResult.Success(openLobbyMatch.MatchId);
                }
                else if (!joinMatchResult.IsValid)
                {
                    transaction.Rollback();
                    return CreateJoinDbFailure(joinMatchResult);
                }
                else
                {
                    unitOfWork.Flush();
                    transaction.Commit();
                }
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

            public static JoinInput Invalid() => new(false, INVALID_ID, string.Empty);

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

            public static ResolvedReadyPlayer Empty() => new(false, default);
        }
    }
}