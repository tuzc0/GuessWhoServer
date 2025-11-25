using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Services;
using log4net;

namespace GuessWho.Services.WCF.Services
{
    public sealed class LobbyNotifier
    {
        private readonly ILog logger;

        private readonly ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>> subscribersByMatch =
            new ConcurrentDictionary<long, ConcurrentDictionary<IMatchCallback, byte>>();

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

        private void Notify(long matchId, Action<IMatchCallback> notifyAction)
        {
            if (!subscribersByMatch.TryGetValue(matchId, out var callbackForMatch))
            {
                return;
            }

            var snapshot = callbackForMatch.Keys.ToArray();

            foreach (var callback in snapshot)
            {
                try
                {
                    notifyAction(callback);
                }
                catch (Exception ex)
                {
                    logger.Warn("Removing faulted callback from subscribers.", ex);
                    callbackForMatch.TryRemove(callback, out _);
                }
            }
        }
    }
}
