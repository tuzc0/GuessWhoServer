using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using System;
using System.Collections.Concurrent;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;

namespace WcfServiceLibraryGuessWho.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = false)]
    public class MatchService : IMatchService
    {
        private readonly MatchData matchData = new MatchData();
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>> subcribersByMatch = new ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>>();

        public void SusbcribeLobby(long matchId)
        {
            var callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
            var callbackForMatch = subcribersByMatch.GetOrAdd(matchId, _ => new ConcurrentDictionary<IMatchCallback, byte>());
            callbackForMatch.TryAdd(callbackChannel, 0);
            var channelObject = (ICommunicationObject)callbackChannel;

            channelObject.Closed += (_, __) =>
            {
                callbackForMatch.TryRemove(callbackChannel, out byte removed);
            };

            channelObject.Faulted += (_, __) =>
            {
                callbackForMatch.TryRemove(callbackChannel, out byte removed);
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

            var dateNow = DateTime.UtcNow;
            var visibilityDefaut = MatchVisibility.Public;
            var statusDefault = MatchStatus.Lobby;
            var modeDefault = "Classic";
            long hostUserId = request.ProfileId;

            try
            {
                var code = CodeGenerator.GenerateNumericCode();

                var createMatchArgs = new CreateMatchArgs
                {
                    UserProfileId = hostUserId,
                    Visibility = (byte)visibilityDefaut,
                    MatchStatus = (byte)statusDefault,
                    Mode = modeDefault,
                    CreateDate = dateNow,
                    MatchCode = code
                };

                MatchDto match = matchData.CreateMatch(createMatchArgs);
                var hostPlayer = matchData.GetMatchPlayers(match.MatchId);

                return new CreateMatchResponse
                {
                    MatchId = match.MatchId,
                    Code = code,
                    StatusId = match.StatusId,
                    Mode = modeDefault,
                    Visibility = (byte)visibilityDefaut,
                    CreateAtUtc = dateNow,
                    Players = hostPlayer
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
            {
                throw Faults.Create("ForeignKey", "Operation violates an existing relation.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                throw Faults.Create("DatabaseTimeout", "The database did not respond in time.");
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                throw Faults.Create("DatabaseConnection", "Unable to connect to the database.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[UserService.RegisterUser] {ex}");
                throw Faults.Create("Unexpected", "Unexpected server error.");
            }
        }

        public JoinMatchResponse JoinMatch(JoinMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create("InvalidRequest", "JoinMatch request cannot be null.");
            }

            var matchCode = request.MatchCode.Trim();
            var userId = request.UserId;
            var user = new JoinMatchArgs
            {
                UserProfileId = userId,
                MatchCode = matchCode,
                JoinedDate = DateTime.Now,
            };

            try
            {
                MATCH match = matchData.GetMatchByCode(user) ?? throw Faults.Create("ErrorJoining", "Match code not found");

                var players = matchData.GetMatchPlayers(match.MATCHID);
                var justJoined = players.FirstOrDefault(p => p.UserId == request.UserId);

                if (justJoined != null)
                {
                    NotifyLobbyJoined(match.MATCHID, justJoined);
                }

                var hostUserId = players.First(p => p.IsHost);

                return new JoinMatchResponse
                {
                    MatchId = match.MATCHID,
                    Code = match.MATCHCODE,
                    StatusId = match.STATUSID,
                    Mode = match.MODE,
                    Visibility = match.VISIBILITYID,
                    CreateAtUtc = match.CREATEDATUTC,
                    HostUserId = userId,
                    Players = players
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsUniqueViolation(ex))
            {
                throw Faults.Create("JoinConflict", "The match is full or you are already joined.");
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsForeignKeyViolation(ex))
            {
                throw Faults.Create("ForeignKey", "Operation violates an existing relation.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[MatchService.JoinMatch] {ex}");
                throw Faults.Create("Unexpected", "Unexpected server error.");
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
