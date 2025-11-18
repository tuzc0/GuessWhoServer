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
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>> subcribersByMatch =
            new ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>>();

        public void SusbcribeLobby(long matchId)
        {
            var callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
            var callbackForMatch = subcribersByMatch.GetOrAdd(
                matchId,
                _ => new ConcurrentDictionary<IMatchCallback, byte>());

            callbackForMatch.TryAdd(callbackChannel, 0);

            var channelObject = (ICommunicationObject)callbackChannel;

            channelObject.Closed += (sender, args) =>
            {
                byte removed;
                callbackForMatch.TryRemove(callbackChannel, out removed);
            };

            channelObject.Faulted += (sender, args) =>
            {
                byte removed;
                callbackForMatch.TryRemove(callbackChannel, out removed);
            };
        }

        public void UnsusbcribeLobby(long matchId)
        {
            if (subcribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                var callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
                callbackForMatch.TryRemove(callbackChannel, out _);
            }
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

                if (hostPlayer == null)
                {
                    throw Faults.Create("NoHost", "Match has no host player.");
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
            throw new NotImplementedException();
        }

        public BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            throw new NotImplementedException();
        }

        protected void NotifyLobbyJoined(long matchId, LobbyPlayerDto player)
        {
            if (!subcribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                return;
            }

            var snapshot = callbackForMatch.Keys.ToArray();

            foreach (var callback in snapshot)
            {
                try
                {
                    callback.OnPlayerJoined(player);
                }
                catch (Exception)
                {
                    callbackForMatch.TryRemove(callback, out _);
                }
            }
        }

        protected void NotifyPlayerLeft(long matchId, LobbyPlayerDto player)
        {
            if (!subcribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                return;
            }

            var snapshot = callbackForMatch.Keys.ToArray();

            foreach (var callback in snapshot)
            {
                try
                {
                    callback.OnPlayerLeft(player);
                }
                catch (Exception)
                {
                    callbackForMatch.TryRemove(callback, out _);
                }
            }
        }

        protected void NotifyPlayerReadyStatusChanged(long matchId, LobbyPlayerDto player)
        {
            if (!subcribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                return;
            }

            var snapshot = callbackForMatch.Keys.ToArray();

            foreach (var callback in snapshot)
            {
                try
                {
                    callback.OnReadyChanged(player);
                }
                catch (Exception)
                {
                    callbackForMatch.TryRemove(callback, out _);
                }
            }
        }
    }
}
