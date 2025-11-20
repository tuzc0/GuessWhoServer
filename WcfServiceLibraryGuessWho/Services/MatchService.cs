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
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = false)]
    public class MatchService : IMatchService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchService));

        private readonly MatchData matchData = new MatchData();
        private readonly LobbyNotifier lobbyNotifier;

        public MatchService()
        {
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

        protected void NotifyLobbyJoined(long matchId, LobbyPlayerDto player)
        {
            lobbyNotifier.NotifyLobbyJoined(matchId, player);
        }

        protected void NotifyPlayerLeft(long matchId, LobbyPlayerDto player)
        {
            lobbyNotifier.NotifyPlayerLeft(matchId, player);
        }

        protected void NotifyPlayerReadyStatusChanged(long matchId, LobbyPlayerDto player)
        {
            lobbyNotifier.NotifyPlayerReadyStatusChanged(matchId, player);
        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "Registration request cannot be null.");
            }

            var dateNowUtc = DateTime.UtcNow;
            var visibilityDefault = MatchVisibility.Public;
            var statusDefault = MatchStatus.Lobby;
            const string MODE_DEFAULT = "Classic";
            long hostUserId = request.ProfileId;

            try
            {
                string code = CodeGenerator.GenerateNumericCode();

                var createMatchArgs = new CreateMatchArgs
                {
                    UserProfileId = hostUserId,
                    Visibility = (byte)visibilityDefault,
                    MatchStatus = (byte)statusDefault,
                    Mode = MODE_DEFAULT,
                    CreateDate = dateNowUtc,
                    MatchCode = code
                };

                MatchDto match = matchData.CreateMatchClassic(createMatchArgs);
                var players = matchData.GetMatchPlayers(match.MatchId);

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
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
            {
                Logger.Error("Foreign key violation in MatchService.CreateMatch.", ex);
                throw Faults.Create("ForeignKey", "Operation violates an existing relation.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database timeout in MatchService.CreateMatch.", ex);
                throw Faults.Create("DatabaseTimeout", "The database did not respond in time.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure in MatchService.CreateMatch.", ex);
                throw Faults.Create("DatabaseConnection", "Unable to connect to the database.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error in MatchService.CreateMatch.", ex);
                throw Faults.Create("Unexpected", "Unexpected server error.");
            }
        }

        private const long INVALID_MATCH_ID = -1;

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "JoinMatch request cannot be null.");
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
                    throw Faults.Create("ErrorJoining", "Match code not found.");
                }

                joinArgs.MatchId = match.MatchId;

                EnsurePlayerCanJoinMatch(joinArgs);

                bool isJoinMatch = matchData.AddPlayerToMatchByCode(joinArgs);

                if (!isJoinMatch)
                {
                    throw Faults.Create("ErrorJoining", "Unable to join the match.");
                }

                var players = matchData.GetMatchPlayers(match.MatchId);
                var justJoined = players.FirstOrDefault(p => p.UserId == userId);

                if (justJoined != null)
                {
                    NotifyLobbyJoined(match.MatchId, justJoined);
                }

                var hostPlayer = players.FirstOrDefault(p => p.IsHost);

                return hostPlayer == null
                    ? throw Faults.Create("NoHost", "Match has no host player.")
                    : new JoinMatchResponse
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
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsUniqueViolation(ex))
            {
                Logger.Warn("Unique constraint violation in MatchService.JoinMatch, likely match is full or already joined.", ex);
                throw Faults.Create("JoinConflict", "The match is full or you are already joined.");
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
            {
                Logger.Error("Foreign key violation in MatchService.JoinMatch.", ex);
                throw Faults.Create("ForeignKey", "Operation violates an existing relation.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database timeout in MatchService.JoinMatch.", ex);
                throw Faults.Create("DatabaseTimeout", "The database did not respond in time.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure in MatchService.JoinMatch.", ex);
                throw Faults.Create("DatabaseConnection", "Unable to connect to the database.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error in MatchService.JoinMatch.", ex);
                throw Faults.Create("Unexpected", "Unexpected server error.");
            }
        }

        private void EnsurePlayerCanJoinMatch(JoinMatchArgs args)
        {
            if (matchData.IsUserInMatch(args.UserProfileId, args.MatchId))
            {
                throw Faults.Create("AlreadyJoined", "User is already in the match.");
            }

            if (matchData.IsUserInActiveMatch(args.UserProfileId, args.MatchId))
            {
                throw Faults.Create("InActiveMatch", "User is already in an active match.");
            }

            if (!matchData.HasAvailableSlotInMatch(args.MatchId))
            {
                throw Faults.Create("MatchFull", "The match has no available slots.");
            }
        }

        public BasicResponse LeaveMatch(LeaveMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "LeaveMatch request cannot be null.");
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
                        throw Faults.Create("MatchNotFound", "La partida no existe.");

                    case LeaveMatchResult.PlayerNotInMatch:

                        Logger.Warn($"LeaveMatch: usuario no pertenece al match. MatchId={request.MatchId}, UserId={request.UserId}");
                        throw Faults.Create("PlayerNotInMatch", "El usuario no pertenece a esta partida.");

                    case LeaveMatchResult.PlayerAlreadyLeft:

                        Logger.Info($"LeaveMatch: usuario ya había salido. MatchId={request.MatchId}, UserId={request.UserId}");
                        throw Faults.Create("PlayerAlreadyLeft", "El usuario ya había salido del match.");

                    default:

                        Logger.Error($"LeaveMatch: resultado desconocido. MatchId={request.MatchId}, UserId={request.UserId}, Result={result}");
                        throw Faults.Create("UnknownError", "No se pudo salir de la partida por un error desconocido.");
                }
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"LeaveMatch: error técnico al intentar salir del match. MatchId={request?.MatchId}, UserId={request?.UserId}",
                    ex);
                throw Faults.Create("TechnicalError", "Ocurrió un error inesperado al salir de la partida.");
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
            catch (Exception ex)
            {
                Logger.Warn(
                    $"LeaveMatch: error al notificar salida en lobby. MatchId={matchId}, UserId={userId}",
                    ex);
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
