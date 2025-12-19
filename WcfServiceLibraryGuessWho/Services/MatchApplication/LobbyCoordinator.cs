using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using log4net;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public sealed class LobbyCoordinator : ILobbyCoordinator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyCoordinator));

        private const long INVALID_MATCH_ID = -1;

        private const string ERROR_INVALID_REQUEST = "InvalidRequest";
        private const string ERROR_JOINING = "ErrorJoining";
        private const string ERROR_NO_HOST = "NoHost";
        private const string ERROR_FOREIGN_KEY = "ForeignKey";
        private const string ERROR_JOIN_CONFLICT = "JoinConflict";
        private const string ERROR_DATABASE_TIMEOUT = "DatabaseTimeout";
        private const string ERROR_DATABASE_CONNECTION = "DatabaseConnection";
        private const string ERROR_DATABASE_UPDATE = "DatabaseUpdate";
        private const string ERROR_DATABASE_SQL = "DatabaseSql";
        private const string ERROR_MATCH_NOT_FOUND = "MatchNotFound";
        private const string ERROR_ALREADY_JOINED = "AlreadyJoined";
        private const string ERROR_MATCH_FULL = "MatchFull";
        private const string ERROR_IN_ACTIVE_MATCH = "InActiveMatch";
        private const string ERROR_UNKNOWN = "UnknownError";
        private const string ERROR_PLAYER_NOT_IN_MATCH = "PlayerNotInMatch";
        private const string ERROR_PLAYER_ALREADY_LEFT = "PlayerAlreadyLeft";
        private const string ERROR_MATCH_NOT_IN_LOBBY = "MatchNotInLobby";

        private const string MESSAGE_JOIN_REQUEST_NULL = "JoinMatch request cannot be null.";
        private const string MESSAGE_MATCH_CODE_NOT_FOUND = "Match code not found.";
        private const string MESSAGE_UNABLE_TO_JOIN = "Unable to join the match.";
        private const string MESSAGE_MATCH_HAS_NO_HOST = "Match has no host player.";
        private const string MESSAGE_FOREIGN_KEY = "Operation violates an existing relation.";
        private const string MESSAGE_JOIN_CONFLICT = "The match is full or you are already joined.";
        private const string MESSAGE_DATABASE_TIMEOUT = "The database did not respond in time.";
        private const string MESSAGE_DATABASE_CONNECTION = "Unable to connect to the database.";
        private const string MESSAGE_DATABASE_UPDATE = "A database update error occurred.";
        private const string MESSAGE_DATABASE_SQL = "A database error occurred.";
        private const string MESSAGE_MATCH_NOT_FOUND = "Match does not exist.";
        private const string MESSAGE_MATCH_NOT_JOINABLE = "This match is no longer joinable (not in Lobby state).";
        private const string MESSAGE_ALREADY_JOINED = "User is already in the match.";
        private const string MESSAGE_MATCH_FULL = "The match is full.";
        private const string MESSAGE_IN_ACTIVE_MATCH = "User is already in an active match.";

        private const string MESSAGE_LEAVE_REQUEST_NULL = "LeaveMatch request cannot be null.";
        private const string MESSAGE_PLAYER_NOT_IN_MATCH = "Player does not belong to this match.";
        private const string MESSAGE_PLAYER_ALREADY_LEFT = "Player had already left the match.";
        private const string MESSAGE_UNKNOWN_LEAVE_ERROR = "Could not leave the match due to an unknown error.";

        private const string MESSAGE_SET_READY_REQUEST_NULL = "SetPlayerReadyStatus request cannot be null.";
        private const string MESSAGE_MATCH_NOT_IN_LOBBY = "The match is no longer in lobby state or has already started.";
        private const string MESSAGE_UNKNOWN_READY_STATUS_ERROR = "Could not update ready status due to an unknown error.";

        private const string LOG_DB_TIMEOUT_JOIN = "Database command timeout in LobbyCoordinator.JoinMatch.";
        private const string LOG_DB_CONNECTION_JOIN = "Database connection failure in LobbyCoordinator.JoinMatch.";
        private const string LOG_DB_UNEXPECTED_JOIN = "Unexpected DB error in LobbyCoordinator.JoinMatch.";

        private const string LOG_DB_TIMEOUT_LEAVE = "Database command timeout in LobbyCoordinator.LeaveMatch.";
        private const string LOG_DB_CONNECTION_LEAVE = "Database connection failure in LobbyCoordinator.LeaveMatch.";
        private const string LOG_DB_UNEXPECTED_LEAVE = "Unexpected DB error in LobbyCoordinator.LeaveMatch.";

        private const string LOG_DB_TIMEOUT_READY = "Database command timeout in LobbyCoordinator.SetPlayerReadyStatus.";
        private const string LOG_DB_CONNECTION_READY = "Database connection failure in LobbyCoordinator.SetPlayerReadyStatus.";
        private const string LOG_DB_UNEXPECTED_READY = "Unexpected DB error in LobbyCoordinator.SetPlayerReadyStatus.";

        private readonly MatchData matchData;
        private readonly ILobbyNotifier lobbyNotifier;

        public LobbyCoordinator(MatchData matchData, ILobbyNotifier lobbyNotifier)
        {
            this.matchData = matchData ?? throw new ArgumentNullException(nameof(matchData));
            this.lobbyNotifier = lobbyNotifier ?? throw new ArgumentNullException(nameof(lobbyNotifier));
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_JOIN_REQUEST_NULL);
            }

            JoinMatchArgs joinMatchArgs = CreateJoinMatchArgs(request);

            try
            {
                MatchDto match = matchData.GetOpenMatchByCode(joinMatchArgs.MatchCode);

                if (match.MatchId == INVALID_MATCH_ID)
                {
                    throw Faults.Create(
                        ERROR_JOINING,
                        MESSAGE_MATCH_CODE_NOT_FOUND);
                }

                joinMatchArgs.MatchId = match.MatchId;

                return JoinNewPlayerToMatch(request, match, joinMatchArgs);
            }
            catch (DbUpdateException ex)
            {
                return HandleDatabaseExceptionForJoinMatch(ex);
            }
            catch (Exception ex)
            {
                return HandleInfrastructureExceptionForJoinMatch(ex);
            }
        }

        private JoinMatchArgs CreateJoinMatchArgs(JoinMatchRequest request)
        {
            string matchCode = (request.MatchCode ?? string.Empty).Trim();
            long userId = request.UserId;

            return new JoinMatchArgs
            {
                UserProfileId = userId,
                MatchCode = matchCode,
                JoinedDate = DateTime.UtcNow
            };
        }

        private JoinMatchResponse JoinNewPlayerToMatch(JoinMatchRequest request, MatchDto match,
            JoinMatchArgs joinArgs)
        {
            JoinMatchResult joinResult = matchData.AddPlayerToMatchByCode(joinArgs);

            switch (joinResult)
            {
                case JoinMatchResult.Success:
                    {
                        var players = matchData.GetMatchPlayers(match.MatchId);
                        LobbyPlayerDto joinedPlayer = players.FirstOrDefault(p => p.UserId == request.UserId);

                        if (joinedPlayer != null)
                        {
                            lobbyNotifier.NotifyLobbyJoined(match.MatchId, joinedPlayer);
                        }

                        LobbyPlayerDto hostPlayer = players.FirstOrDefault(p => p.IsHost);

                        if (hostPlayer == null)
                        {
                            throw Faults.Create(
                                ERROR_NO_HOST,
                                MESSAGE_MATCH_HAS_NO_HOST);
                        }

                        return new JoinMatchResponse
                        {
                            MatchId = match.MatchId,
                            Code = match.Code,
                            StatusId = match.StatusId,
                            Mode = match.Mode,
                            Visibility = match.VisibilityId,
                            CreateAtUtc = match.CreateAtUtc,
                            HostUserId = hostPlayer.UserId,
                            Players = players
                        };
                    }

                case JoinMatchResult.MatchNotFound:

                    throw Faults.Create(
                        ERROR_MATCH_NOT_FOUND,
                        MESSAGE_MATCH_NOT_FOUND);

                case JoinMatchResult.MatchNotJoinable:

                    throw Faults.Create(
                        ERROR_JOINING,
                        MESSAGE_MATCH_NOT_JOINABLE);

                case JoinMatchResult.PlayerAlreadyInMatch:

                    throw Faults.Create(
                        ERROR_ALREADY_JOINED,
                        MESSAGE_ALREADY_JOINED);

                case JoinMatchResult.GuestSlotTaken:

                    throw Faults.Create(
                        ERROR_MATCH_FULL,
                        MESSAGE_MATCH_FULL);

                case JoinMatchResult.InOtherActiveMatch:

                    throw Faults.Create(
                        ERROR_IN_ACTIVE_MATCH,
                        MESSAGE_IN_ACTIVE_MATCH);

                default:

                    Logger.ErrorFormat("JoinNewPlayerToMatch: unknown join result. UserId={0}, MatchId={1}, Result={2}",
                        request.UserId, match.MatchId, joinResult);
                    throw Faults.Create(
                        ERROR_JOINING,
                        MESSAGE_UNABLE_TO_JOIN);
            }
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_LEAVE_REQUEST_NULL);
            }

            var leaveArgs = new MatchPlayerArgs
            {
                UserProfileId = request.UserId,
                MatchId = request.MatchId
            };

            try
            {
                LeaveMatchResult result = matchData.LeaveMatch(leaveArgs);

                switch (result)
                {
                    case LeaveMatchResult.Success:

                        NotifyPlayerLeftSafe(request.MatchId, request.UserId);

                        return new BasicResponse
                        {
                            Success = true
                        };

                    case LeaveMatchResult.MatchNotFound:

                        Logger.WarnFormat("LeaveMatch: match not found. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        throw Faults.Create(
                            ERROR_MATCH_NOT_FOUND,
                            MESSAGE_MATCH_NOT_FOUND);

                    case LeaveMatchResult.PlayerNotInMatch:

                        Logger.WarnFormat("LeaveMatch: player not in match. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        throw Faults.Create(
                            ERROR_PLAYER_NOT_IN_MATCH,
                            MESSAGE_PLAYER_NOT_IN_MATCH);

                    case LeaveMatchResult.PlayerAlreadyLeft:

                        Logger.InfoFormat("LeaveMatch: player already left. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        throw Faults.Create(
                            ERROR_PLAYER_ALREADY_LEFT,
                            MESSAGE_PLAYER_ALREADY_LEFT);

                    default:

                        Logger.ErrorFormat("LeaveMatch: unknown result. MatchId={0}, UserId={1}, Result={2}",
                            request.MatchId, request.UserId, result);
                        throw Faults.Create(
                            ERROR_UNKNOWN,
                            MESSAGE_UNKNOWN_LEAVE_ERROR);
                }
            }
            catch (DbUpdateException ex)
            {
                return HandleDatabaseExceptionForLeaveMatch(ex);
            }
            catch (Exception ex)
            {
                return HandleInfrastructureExceptionForLeaveMatch(ex);
            }
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_SET_READY_REQUEST_NULL);
            }

            var setReadyArgs = new MatchPlayerArgs
            {
                UserProfileId = request.UserId,
                MatchId = request.MatchId
            };

            try
            {
                MarkReadyResult result = matchData.MarkPlayerAsReady(setReadyArgs);

                switch (result)
                {
                    case MarkReadyResult.Success:

                        NotifyPlayerReadySafe(request.MatchId, request.UserId);

                        return new BasicResponse
                        {
                            Success = true
                        };

                    case MarkReadyResult.PlayerNotFound:

                        Logger.WarnFormat("SetPlayerReadyStatus: player not in match. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        throw Faults.Create(
                            ERROR_PLAYER_NOT_IN_MATCH,
                            MESSAGE_PLAYER_NOT_IN_MATCH);

                    case MarkReadyResult.PlayerAlreadyLeft:

                        Logger.InfoFormat("SetPlayerReadyStatus: player already left. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        throw Faults.Create(
                            ERROR_PLAYER_ALREADY_LEFT,
                            MESSAGE_PLAYER_ALREADY_LEFT);

                    case MarkReadyResult.MatchNotInLobby:

                        Logger.WarnFormat("SetPlayerReadyStatus: match not in lobby state. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        throw Faults.Create(
                            ERROR_MATCH_NOT_IN_LOBBY,
                            MESSAGE_MATCH_NOT_IN_LOBBY);

                    default:

                        Logger.ErrorFormat("SetPlayerReadyStatus: unknown result. MatchId={0}, UserId={1}, Result={2}",
                            request.MatchId, request.UserId, result);
                        throw Faults.Create(
                            ERROR_UNKNOWN,
                            MESSAGE_UNKNOWN_READY_STATUS_ERROR);
                }
            }
            catch (DbUpdateException ex)
            {
                return HandleDatabaseExceptionForSetReady(ex);
            }
            catch (Exception ex)
            {
                return HandleInfrastructureExceptionForSetReady(ex);
            }
        }

        public void SubscribeLobby(long matchId)
        {
            lobbyNotifier.SubscribeLobby(matchId);
        }

        public void UnsubscribeLobby(long matchId)
        {
            lobbyNotifier.UnsubscribeLobby(matchId);
        }

        private void NotifyPlayerLeftSafe(long matchId, long userId)
        {
            var playerLeft = new LobbyNotificationDto
            {
                MatchId = matchId,
                UserId = userId,
                OperationName = "LeaveMatch"
            };

            lobbyNotifier.NotifyLobbyNotificationSafe(playerLeft,
                lobbyNotifier.NotifyPlayerLeft);
        }

        private void NotifyPlayerReadySafe(long matchId, long userId)
        {
            var playerReady = new LobbyNotificationDto
            {
                MatchId = matchId,
                UserId = userId,
                OperationName = "SetPlayerReadyStatus"
            };

            lobbyNotifier.NotifyLobbyNotificationSafe(playerReady,
                lobbyNotifier.NotifyPlayerReadyStatusChanged);
        }

        private JoinMatchResponse HandleDatabaseExceptionForJoinMatch(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_JOIN, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_JOIN, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);

                    case SqlErrorKind.UniqueViolation:
                        Logger.Warn(
                            "Unique constraint violation in LobbyCoordinator.JoinMatch, likely match is full or already joined.",
                            ex);
                        throw Faults.Create(ERROR_JOIN_CONFLICT, MESSAGE_JOIN_CONFLICT);

                    case SqlErrorKind.ForeignKeyViolation:
                        Logger.Error("Foreign key violation in LobbyCoordinator.JoinMatch.", ex);
                        throw Faults.Create(ERROR_FOREIGN_KEY, MESSAGE_FOREIGN_KEY);
                }
            }

            Logger.Fatal("Database update error in LobbyCoordinator.JoinMatch.", ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private JoinMatchResponse HandleInfrastructureExceptionForJoinMatch(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_JOIN, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_JOIN, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_JOIN, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }

        private BasicResponse HandleDatabaseExceptionForLeaveMatch(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_LEAVE, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_LEAVE, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal("Database update error in LobbyCoordinator.LeaveMatch.", ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private BasicResponse HandleInfrastructureExceptionForLeaveMatch(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_LEAVE, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_LEAVE, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_LEAVE, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }

        private BasicResponse HandleDatabaseExceptionForSetReady(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_READY, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_READY, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal("Database update error in LobbyCoordinator.SetPlayerReadyStatus.", ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private BasicResponse HandleInfrastructureExceptionForSetReady(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_READY, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_READY, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_READY, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }
    }
}
