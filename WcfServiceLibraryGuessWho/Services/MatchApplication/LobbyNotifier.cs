using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public class LobbyNotifier : ILobbyNotifier
    {
        private readonly ILog logger;

        private readonly ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, 
            byte>> subscribersByMatch = new ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>>();

        public LobbyNotifier(ILog logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SubscribeLobby(long matchId)
        {
            var callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();

            var callbackForMatch = subscribersByMatch.GetOrAdd(
                matchId,
                _ => new ConcurrentDictionary<IMatchCallback, byte>());

            callbackForMatch.TryAdd(callbackChannel, 0);

            var channelObject = (ICommunicationObject)callbackChannel;

            channelObject.Closed += (sender, args) =>
            {
                callbackForMatch.TryRemove(callbackChannel, out _);
            };

            channelObject.Faulted += (sender, args) =>
            {
                callbackForMatch.TryRemove(callbackChannel, out _);
            };
        }

        public void UnsubscribeLobby(long matchId)
        {
            if (subscribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                var callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
                callbackForMatch.TryRemove(callbackChannel, out _);
            }
        }

        public void NotifyLobbyJoined(long matchId, LobbyPlayerDto player)
        {
            Notify(matchId, callback => callback.OnPlayerJoined(player));
        }

        public void NotifyPlayerLeft(long matchId, LobbyPlayerDto player)
        {
            Notify(matchId, callback => callback.OnPlayerLeft(player));
        }

        public void NotifyPlayerReadyStatusChanged(long matchId, LobbyPlayerDto player)
        {
            Notify(matchId, callback => callback.OnReadyChanged(player));
        }

        public void NotifyGameStarted(long matchId)
        {
            Notify(matchId, callback => callback.OnGameStarted(matchId));
        }

        public void NotifyGameEnded(long matchId, long winnerUserId)
        {
            Notify(matchId, callback => callback.OnGameEnded(matchId, winnerUserId));
        }

        public void NotifySecretCharacterChosen(long matchId, long userId)
        {
            Notify(matchId, callback => callback.OnSecretCharacterChosen(matchId, userId));
        }

        public void NotifyAllSecretCharactersChosen(long matchId)
        {
            Notify(matchId, callback => callback.OnAllSecretCharactersChosen(matchId));
        }

        public void NotifyLobbyNotificationSafe(LobbyNotificationDto lobbyNotification,
            Action<long, LobbyPlayerDto> notifyAction)
        {
            if (lobbyNotification == null)
            {
                throw new ArgumentNullException(nameof(lobbyNotification));
            }

            var player = new LobbyPlayerDto
            {
                UserId = lobbyNotification.UserId
            };

            try
            {
                notifyAction(lobbyNotification.MatchId, player);
            }
            catch (TimeoutException ex)
            {
                logger.WarnFormat("{0}: timeout while notifying lobby. MatchId={1}, UserId={2}",
                    lobbyNotification.OperationName, lobbyNotification.MatchId,
                    lobbyNotification.UserId, ex);
            }
            catch (CommunicationException ex)
            {
                logger.WarnFormat(
                    "{0}: communication error while notifying lobby. MatchId={1}, UserId={2}",
                    lobbyNotification.OperationName, lobbyNotification.MatchId,
                    lobbyNotification.UserId, ex);
            }
            catch (Exception ex)
            {
                logger.WarnFormat("{0}: unexpected error while notifying lobby. MatchId={1}, UserId={2}",
                    lobbyNotification.OperationName, lobbyNotification.MatchId,
                    lobbyNotification.UserId, ex);
            }
        }

        private void Notify(long matchId, Action<IMatchCallback> notifyAction)
        {
            if (notifyAction == null)
            {
                throw new ArgumentNullException(nameof(notifyAction));
            }

            if (!subscribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                return;
            }

            var snapshot = callbackForMatch.Keys.ToArray();

            foreach (IMatchCallback callback in snapshot)
            {
                try
                {
                    notifyAction(callback);
                }
                catch (TimeoutException ex)
                {
                    logger.Warn(string.Format("LobbyNotifier.Notify: " +
                        "timeout while notifying callback. MatchId={0}", matchId), ex);
                    callbackForMatch.TryRemove(callback, out _);
                }
                catch (CommunicationException ex)
                {
                    logger.Warn(string.Format("LobbyNotifier.Notify: " +
                        "communication error while notifying callback. MatchId={0}", matchId), ex);
                    callbackForMatch.TryRemove(callback, out _);
                }
                catch (ObjectDisposedException ex)
                {
                    logger.Warn(string.Format("LobbyNotifier.Notify: " +
                        "disposed callback while notifying. MatchId={0}", matchId), ex);
                    callbackForMatch.TryRemove(callback, out _);
                }
                catch (Exception ex)
                {
                    logger.Warn(string.Format("LobbyNotifier.Notify: " +
                        "unexpected error while notifying callback. " +
                        "MatchId={0}", matchId), ex);
                    callbackForMatch.TryRemove(callback, out _);
                }
            }
        }
    }
}
