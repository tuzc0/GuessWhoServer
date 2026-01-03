using GuessWhoContracts.Services;
using GuessWhoDataAccess.Data.Factories;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace GuessWhoServices.Infrastructure
{
    public interface IMatchDisconnectHandler
    {
        void HandleMatchDisconnect(long userId, DateTime nowUtc);
    }

    public sealed class LobbySubscriptionStore : ILobbySubscriptionStore
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbySubscriptionStore));

        private const long INVALID_ID = 0;
        private const long NO_MATCH_ID = 0;

        private sealed class SessionEntry
        {
            private long currentMatchId;

            public SessionEntry(ICommunicationObject channelObject, long userId, long matchId)
            {
                ChannelObject = channelObject ?? throw new ArgumentNullException(nameof(channelObject));
                UserId = userId;
                currentMatchId = matchId;
            }

            public ICommunicationObject ChannelObject { get; }

            public long UserId { get; }

            public long MatchId => Interlocked.Read(ref currentMatchId);

            public long SetMatchId(long matchId)
            {
                return Interlocked.Exchange(ref currentMatchId, matchId);
            }
        }

        private readonly IMatchDisconnectHandler disconnectHandler;

        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, IMatchCallback>> subscribersByMatchId =
            new ConcurrentDictionary<long, ConcurrentDictionary<string, IMatchCallback>>();

        private readonly ConcurrentDictionary<string, SessionEntry> sessionEntries =
            new ConcurrentDictionary<string, SessionEntry>(StringComparer.Ordinal);

        public LobbySubscriptionStore(IMatchDisconnectHandler disconnectHandler)
        {
            this.disconnectHandler = disconnectHandler ?? throw new ArgumentNullException(nameof(disconnectHandler));
        }

        public bool Subscribe(long matchId, long userId, IMatchCallback callbackChannel)
        {
            if (matchId <= NO_MATCH_ID || userId <= INVALID_ID || callbackChannel == null)
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

            SessionEntry sessionEntry = EnsureSessionEntry(sessionId, channelObject, userId, matchId);

            long previousMatchId = sessionEntry.SetMatchId(matchId);

            if (previousMatchId > NO_MATCH_ID && previousMatchId != matchId)
            {
                RemoveSubscriberFromMatch(previousMatchId, sessionId);
            }

            ConcurrentDictionary<string, IMatchCallback> subscribersForMatch = subscribersByMatchId.GetOrAdd(
                matchId,
                _ => new ConcurrentDictionary<string, IMatchCallback>(StringComparer.Ordinal));

            subscribersForMatch[sessionId] = callbackChannel;

            return true;
        }

        public void Unsubscribe(long matchId, IMatchCallback callbackChannel)
        {
            if (matchId <= NO_MATCH_ID || callbackChannel == null)
            {
                return;
            }

            string sessionId = TryGetSessionId(callbackChannel);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            RemoveSubscriberFromMatch(matchId, sessionId);

            if (sessionEntries.TryGetValue(sessionId, out SessionEntry sessionEntry))
            {
                if (sessionEntry.MatchId == matchId)
                {
                    sessionEntry.SetMatchId(NO_MATCH_ID);
                }
            }
        }

        public IReadOnlyList<IMatchCallback> GetSubscribers(long matchId)
        {
            if (matchId <= NO_MATCH_ID)
            {
                return Array.Empty<IMatchCallback>();
            }

            if (!subscribersByMatchId.TryGetValue(matchId, out ConcurrentDictionary<string, IMatchCallback> subscribersForMatch))
            {
                return Array.Empty<IMatchCallback>();
            }

            return subscribersForMatch.Values.ToList();
        }

        private SessionEntry EnsureSessionEntry(string sessionId, ICommunicationObject channelObject, long userId, long matchId)
        {
            return sessionEntries.GetOrAdd(
                sessionId,
                _ =>
                {
                    var entry = new SessionEntry(channelObject, userId, matchId);
                    AttachAutoCleanupOnce(sessionId, entry);
                    return entry;
                });
        }

        private void AttachAutoCleanupOnce(string sessionId, SessionEntry sessionEntry)
        {
            void Cleanup(object sender, EventArgs args) => CleanupSession(sessionId);

            sessionEntry.ChannelObject.Closed += Cleanup;
            sessionEntry.ChannelObject.Faulted += Cleanup;
        }

        private void CleanupSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            if (sessionEntries.TryRemove(sessionId, out SessionEntry removedEntry))
            {
                long lastKnownMatchId = removedEntry.MatchId;

                if (lastKnownMatchId > NO_MATCH_ID)
                {
                    RemoveSubscriberFromMatch(lastKnownMatchId, sessionId);
                }
                else
                {
                    RemoveSubscriberFromAllMatches(sessionId);
                }

                TryHandleDisconnect(removedEntry.UserId);
                return;
            }
            RemoveSubscriberFromAllMatches(sessionId);
        }

        private void RemoveSubscriberFromMatch(long matchId, string sessionId)
        {
            if (matchId <= NO_MATCH_ID || string.IsNullOrWhiteSpace(sessionId))
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

        private void RemoveSubscriberFromAllMatches(string sessionId)
        {
            foreach (KeyValuePair<long, ConcurrentDictionary<string, IMatchCallback>> kvp in subscribersByMatchId)
            {
                kvp.Value.TryRemove(sessionId, out _);

                if (kvp.Value.IsEmpty)
                {
                    subscribersByMatchId.TryRemove(kvp.Key, out _);
                }
            }
        }

        private void TryHandleDisconnect(long userId)
        {
            if (userId <= INVALID_ID)
            {
                return;
            }

            try
            {
                disconnectHandler.HandleMatchDisconnect(userId, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Logger.Error("LobbySubscriptionStore.HandleDisconnect failed.", ex);
            }
        }

        private static string TryGetSessionId(IMatchCallback callbackChannel)
        {
            return (callbackChannel as IContextChannel)?.SessionId ?? string.Empty;
        }

        private sealed class MatchDisconnectHandler : IMatchDisconnectHandler
        {
            private const long INVALID_ID = 0;

            private readonly IGuessWhoUnitOfWorkFactory _unitOfWorkFactory;
            private readonly ILog _logger;

            public MatchDisconnectHandler(IGuessWhoUnitOfWorkFactory unitOfWorkFactory, ILog logger)
            {
                _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public void HandleMatchDisconnect(long userId, DateTime nowUtc)
            {
                if (userId <= INVALID_ID)
                {
                    return;
                }

                try
                {
                    using IGuessWhoUnitOfWork unitOfWork = _unitOfWorkFactory.Create();
                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    var result = unitOfWork.Matches.HandleDisconnect(userId, nowUtc);

                    if (!result.IsSuccess)
                    {
                        transaction.Rollback();
                        return;
                    }

                    unitOfWork.Flush();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _logger.Error("MatchDisconnectHandler.HandleMatchDisconnect failed.", ex);
                }
            }
        }
    }
}
