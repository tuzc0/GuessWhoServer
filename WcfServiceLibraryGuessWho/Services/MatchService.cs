using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;

namespace GuessWho.Services.WCF.Services

{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = false)]
    public sealed class MatchService : IMatchService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchService));

        private const long INVALID_MATCH_ID = -1;

        private const string MODE_CLASSIC = "Classic";

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
        private const string ERROR_PLAYER_NOT_IN_MATCH = "PlayerNotInMatch";
        private const string ERROR_PLAYER_ALREADY_LEFT = "PlayerAlreadyLeft";
        private const string ERROR_UNKNOWN = "UnknownError";

        private const string MESSAGE_REQUEST_NULL = "Request cannot be null.";
        private const string MESSAGE_JOIN_REQUEST_NULL = "JoinMatch request cannot be null.";
        private const string MESSAGE_LEAVE_REQUEST_NULL = "LeaveMatch request cannot be null.";
        private const string MESSAGE_MATCH_CODE_NOT_FOUND = "Match code not found.";
        private const string MESSAGE_UNABLE_TO_JOIN = "Unable to join the match.";
        private const string MESSAGE_MATCH_HAS_NO_HOST = "Match has no host player.";
        private const string MESSAGE_FOREIGN_KEY = "Operation violates an existing relation.";
        private const string MESSAGE_JOIN_CONFLICT = "The match is full or you are already joined.";
        private const string MESSAGE_DATABASE_TIMEOUT = "The database did not respond in time.";
        private const string MESSAGE_DATABASE_CONNECTION = "Unable to connect to the database.";
        private const string MESSAGE_DATABASE_UPDATE = "A database update error occurred.";
        private const string MESSAGE_DATABASE_SQL = "A database error occurred.";

        private const string MESSAGE_MATCH_NOT_FOUND = "La partida no existe.";
        private const string MESSAGE_PLAYER_NOT_IN_MATCH = "El usuario no pertenece a esta partida.";
        private const string MESSAGE_PLAYER_ALREADY_LEFT = "El usuario ya había salido del match.";
        private const string MESSAGE_UNKNOWN_LEAVE_ERROR = "No se pudo salir de la partida por un error desconocido.";

        private readonly MatchData matchData;
        private readonly LobbyNotifier lobbyNotifier;

        public MatchService()
        {
            matchData = new MatchData();
            lobbyNotifier = new LobbyNotifier(Logger);
        }

        public void SusbcribeLobby(long matchId)
        {
            lobbyNotifier.SubscribeLobby(matchId);
        }

        public void UnsusbcribeLobby(long matchId)
        {
            lobbyNotifier.UnsubscribeLobby(matchId);
        }

        private void NotifyLobbyJoined(long matchId, LobbyPlayerDto player)
        {
            lobbyNotifier.NotifyLobbyJoined(matchId, player);
        }

        private void NotifyPlayerLeft(long matchId, LobbyPlayerDto player)
        {
            lobbyNotifier.NotifyPlayerLeft(matchId, player);
        }

        private void NotifyPlayerReadyStatusChanged(long matchId, LobbyPlayerDto player)
        {
            lobbyNotifier.NotifyPlayerReadyStatusChanged(matchId, player);
        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(ERROR_INVALID_REQUEST, MESSAGE_REQUEST_NULL);
            }

            var dateNowUtc = DateTime.UtcNow;
            var visibilityDefault = MatchVisibility.Public;
            var statusDefault = MatchStatus.Lobby;
            long hostUserId = request.ProfileId;

            try
            {
                string matchCode = CodeGenerator.GenerateNumericCode();

                var createMatchArgs = new CreateMatchArgs
                {
                    UserProfileId = hostUserId,
                    Visibility = (byte)visibilityDefault,
                    MatchStatus = (byte)statusDefault,
                    Mode = MODE_CLASSIC,
                    CreateDate = dateNowUtc,
                    MatchCode = matchCode
                };

                Logger.InfoFormat(
                    "CreateMatch request received. HostUserId={0}, Visibility={1}, Status={2}, Mode={3}, MatchCode={4}, CreateDateUtc={5:o}",
                    createMatchArgs.UserProfileId,
                    createMatchArgs.Visibility,
                    createMatchArgs.MatchStatus,
                    createMatchArgs.Mode,
                    createMatchArgs.MatchCode,
                    createMatchArgs.CreateDate);

                MatchDto match = matchData.CreateMatchClassic(createMatchArgs);
                var players = matchData.GetMatchPlayers(match.MatchId);

                Logger.InfoFormat(
                    "CreateMatch created match successfully. MatchId={0}, Code={1}, StatusId={2}, VisibilityId={3}, Mode={4}, HostUserId={5}, PlayersCount={6}",
                    match.MatchId,
                    match.Code,
                    match.StatusId,
                    match.VisibilityId,
                    match.Mode,
                    hostUserId,
                    players == null ? 0 : players.Count);

                return new CreateMatchResponse
                {
                    MatchId = match.MatchId,
                    Code = match.Code,
                    StatusId = match.StatusId,
                    Mode = match.Mode,
                    Visibility = match.VisibilityId,
                    CreateAtUtc = match.CreateAtUtc,
                    Players = players
                };
            }
            catch (DbUpdateException dbUpdateException) when (SqlExceptionInspector.IsForeignKeyViolation(dbUpdateException))
            {
                Logger.Error("Foreign key violation in MatchService.CreateMatch.", dbUpdateException);
                throw Faults.Create(ERROR_FOREIGN_KEY, MESSAGE_FOREIGN_KEY);
            }
            catch (DbUpdateException dbUpdateException)
            {
                Logger.Fatal("Database update error in MatchService.CreateMatch.", dbUpdateException);
                throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
            }
            catch (SqlException sqlException) when (SqlExceptionInspector.IsCommandTimeout(sqlException))
            {
                Logger.Fatal("Database timeout in MatchService.CreateMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);
            }
            catch (SqlException sqlException) when (SqlExceptionInspector.IsConnectionFailure(sqlException))
            {
                Logger.Fatal("Database connection failure in MatchService.CreateMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
            }
            catch (SqlException sqlException)
            {
                Logger.Fatal("Database SQL error in MatchService.CreateMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
            }
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(ERROR_INVALID_REQUEST, 
                    MESSAGE_JOIN_REQUEST_NULL);
            }

            string matchCode = (request.MatchCode ?? string.Empty).Trim();
            long userId = request.UserId;

            var joinArgs = new JoinMatchArgs
            {
                UserProfileId = userId,
                MatchCode = matchCode,
                JoinedDate = DateTime.UtcNow
            };

            try
            {
                MatchDto match = matchData.GetOpenMatchByCode(matchCode);

                if (match.MatchId == INVALID_MATCH_ID)
                {
                    throw Faults.Create(ERROR_JOINING, 
                        MESSAGE_MATCH_CODE_NOT_FOUND);
                }

                joinArgs.MatchId = match.MatchId;

                return JoinNewPlayerToMatch(request, match, joinArgs);
            }
            catch (DbUpdateException dbUpdateException) when (SqlExceptionInspector.IsUniqueViolation(dbUpdateException))
            {
                Logger.Warn("Unique constraint violation in MatchService.JoinMatch, likely match is full or already joined.", dbUpdateException);
                throw Faults.Create(ERROR_JOIN_CONFLICT, MESSAGE_JOIN_CONFLICT);
            }
            catch (DbUpdateException dbUpdateException) when (SqlExceptionInspector.IsForeignKeyViolation(dbUpdateException))
            {
                Logger.Error("Foreign key violation in MatchService.JoinMatch.", dbUpdateException);
                throw Faults.Create(ERROR_FOREIGN_KEY, MESSAGE_FOREIGN_KEY);
            }
            catch (DbUpdateException dbUpdateException)
            {
                Logger.Fatal("Database update error in MatchService.JoinMatch.", dbUpdateException);
                throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
            }
            catch (SqlException sqlException) when (SqlExceptionInspector.IsCommandTimeout(sqlException))
            {
                Logger.Fatal("Database timeout in MatchService.JoinMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);
            }
            catch (SqlException sqlException) when (SqlExceptionInspector.IsConnectionFailure(sqlException))
            {
                Logger.Fatal("Database connection failure in MatchService.JoinMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
            }
            catch (SqlException sqlException)
            {
                Logger.Fatal("Database SQL error in MatchService.JoinMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
            }
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(ERROR_INVALID_REQUEST, 
                    MESSAGE_LEAVE_REQUEST_NULL);
            }

            var leaveArgs = new LeaveMatchArgs
            {
                UserProfileId = request.UserId,
                MatchId = request.MatchId,
                LeftDate = DateTime.UtcNow
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
                        Logger.Warn($"LeaveMatch: match no encontrado. MatchId={request.MatchId}, UserId={request.UserId}");
                        throw Faults.Create(ERROR_MATCH_NOT_FOUND, MESSAGE_MATCH_NOT_FOUND);

                    case LeaveMatchResult.PlayerNotInMatch:
                        Logger.Warn($"LeaveMatch: usuario no pertenece al match. MatchId={request.MatchId}, UserId={request.UserId}");
                        throw Faults.Create(ERROR_PLAYER_NOT_IN_MATCH, MESSAGE_PLAYER_NOT_IN_MATCH);

                    case LeaveMatchResult.PlayerAlreadyLeft:
                        Logger.Info($"LeaveMatch: usuario ya había salido. MatchId={request.MatchId}, UserId={request.UserId}");
                        throw Faults.Create(ERROR_PLAYER_ALREADY_LEFT, MESSAGE_PLAYER_ALREADY_LEFT);

                    default:
                        Logger.Error($"LeaveMatch: resultado desconocido. MatchId={request.MatchId}, UserId={request.UserId}, Result={result}");
                        throw Faults.Create(ERROR_UNKNOWN, MESSAGE_UNKNOWN_LEAVE_ERROR);
                }
            }
            catch (DbUpdateException dbUpdateException)
            {
                Logger.Fatal("Database update error in MatchService.LeaveMatch.", dbUpdateException);
                throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
            }
            catch (SqlException sqlException) when (SqlExceptionInspector.IsCommandTimeout(sqlException))
            {
                Logger.Fatal("Database timeout in MatchService.LeaveMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);
            }
            catch (SqlException sqlException) when (SqlExceptionInspector.IsConnectionFailure(sqlException))
            {
                Logger.Fatal("Database connection failure in MatchService.LeaveMatch.", sqlException);
                throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
            }
            catch (SqlException sqlException)
            {
                Logger.Error(
                    $"LeaveMatch: database SQL error al intentar salir del match. MatchId={request.MatchId}, UserId={request.UserId}",
                    sqlException);

                throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
            }
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
                        var joinedPlayer = players.FirstOrDefault(p => p.UserId == request.UserId);

                        if (joinedPlayer != null)
                        {
                            NotifyLobbyJoined(match.MatchId, joinedPlayer);
                        }

                        var hostPlayer = players.FirstOrDefault(p => p.IsHost);
                        
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
                        "This match is no longer joinable (not in Lobby state).");

                case JoinMatchResult.PlayerAlreadyInMatch:

                    throw Faults.Create(
                        "AlreadyJoined", 
                        "User is already in the match.");

                case JoinMatchResult.GuestSlotTaken:
                    
                    throw Faults.Create(
                        "MatchFull", 
                        "The match has no available slots.");

                case JoinMatchResult.InOtherActiveMatch:

                    throw Faults.Create(
                        "InActiveMatch", 
                        "User is already in an active match.");

                default:
                    Logger.ErrorFormat(
                        "JoinNewPlayerToMatch: unknown join result. UserId={0}, MatchId={1}, Result={2}",
                        request.UserId,
                        match.MatchId,
                        joinResult);

                    throw Faults.Create(
                        ERROR_JOINING, 
                        MESSAGE_UNABLE_TO_JOIN);
            }
        }

        private void NotifyPlayerLeftSafe(long matchId, long userId)
        {
            try
            {
                var player = new LobbyPlayerDto
                {
                    UserId = userId
                };

                NotifyPlayerLeft(matchId, player);
            }
            catch (Exception notificationException)
            {
                Logger.Warn(
                    $"LeaveMatch: error al notificar salida en lobby. MatchId={matchId}, UserId={userId}",
                    notificationException);
            }
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
