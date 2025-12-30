using GuessWhoContracts.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace GuessWhoServices.Infrastructure
{
    public sealed class LobbySubscriptionStore : ILobbySubscriptionStore
    {
        private sealed class SessionEntry
        {
            public ICommunicationObject ChannelObject { get; }

            public SessionEntry(ICommunicationObject channelObject)
            {
                ChannelObject = channelObject ?? throw new ArgumentNullException(nameof(channelObject));
            }
        }

        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, IMatchCallback>> subscribersByMatchId =
            new ConcurrentDictionary<long, ConcurrentDictionary<string, IMatchCallback>>();

        private readonly ConcurrentDictionary<string, SessionEntry> sessionEntries =
            new ConcurrentDictionary<string, SessionEntry>();

        public bool Subscribe(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= 0 || callbackChannel == null)
            {
                return false;
            }

            string sessionId = TryGetSessionId(callbackChannel);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            if (callbackChannel is not ICommunicationObject channelObject)
            {
                return false;
            }

            EnsureSessionEntry(sessionId, channelObject);

            ConcurrentDictionary<string, IMatchCallback> subscribersForMatch = subscribersByMatchId.GetOrAdd(
                matchId,
                _ => new ConcurrentDictionary<string, IMatchCallback>());

            subscribersForMatch[sessionId] = callbackChannel;

            return true;
        }

        public void Unsubscribe(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= 0 || callbackChannel == null)
            {
                return;
            }

            string sessionId = TryGetSessionId(callbackChannel);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            if (subscribersByMatchId.TryGetValue(matchId, out ConcurrentDictionary<string, IMatchCallback> subscribersForMatch))
            {
                subscribersForMatch.TryRemove(sessionId, out _);

                if (subscribersForMatch.IsEmpty)
                {
                    subscribersByMatchId.TryRemove(matchId, out _);
                }
            }
        }

        public IReadOnlyList<IMatchCallback> GetSubscribers(long matchId)
        {
            if (matchId <= 0)
            {
                return Array.Empty<IMatchCallback>();
            }

            if (!subscribersByMatchId.TryGetValue(matchId, out ConcurrentDictionary<string, IMatchCallback> subscribersForMatch))
            {
                return Array.Empty<IMatchCallback>();
            }

            return subscribersForMatch.Values.ToList();
        }

        private void EnsureSessionEntry(string sessionId, ICommunicationObject channelObject)
        {
            sessionEntries.GetOrAdd(
                sessionId,
                _ =>
                {
                    var entry = new SessionEntry(channelObject);
                    AttachAutoCleanupOnce(sessionId, entry);
                    return entry;
                });
        }

        private void AttachAutoCleanupOnce(string sessionId, SessionEntry entry)
        {
            void Cleanup(object sender, EventArgs args) => CleanupSession(sessionId);

            entry.ChannelObject.Closed += Cleanup;
            entry.ChannelObject.Faulted += Cleanup;
        }

        private void CleanupSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            sessionEntries.TryRemove(sessionId, out _);

            foreach (KeyValuePair<long, ConcurrentDictionary<string, IMatchCallback>> kvp in subscribersByMatchId)
            {
                ConcurrentDictionary<string, IMatchCallback> subscribersForMatch = kvp.Value;

                subscribersForMatch.TryRemove(sessionId, out _);

                if (subscribersForMatch.IsEmpty)
                {
                    subscribersByMatchId.TryRemove(kvp.Key, out _);
                }
            }
        }

        private static string TryGetSessionId(IMatchCallback callbackChannel)
        {
            return (callbackChannel as IContextChannel)?.SessionId ?? string.Empty;
        }
    }
}