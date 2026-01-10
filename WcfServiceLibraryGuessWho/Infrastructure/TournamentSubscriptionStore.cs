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
    public interface ITournamentDisconnectHandler
    {
        void HandleTournamentDisconnect(long userId, DateTime nowUtc);
    }

    public sealed class TournamentSubscriptionStore : ITournamentSubscriptionStore
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TournamentSubscriptionStore));

        private const long INVALID_ID = 0;
        private const long NO_TOURNAMENT_ID = 0;

        private sealed class SessionEntry
        {
            private long currentTournamentId;

            public SessionEntry(ICommunicationObject channelObject, long userId, long tournamentId)
            {
                ChannelObject = channelObject ?? throw new ArgumentNullException(nameof(channelObject));
                UserId = userId;
                currentTournamentId = tournamentId;
            }

            public ICommunicationObject ChannelObject { get; }

            public long UserId { get; }

            public long TournamentId => Interlocked.Read(ref currentTournamentId);

            public long SetTournamentId(long tournamentId)
            {
                return Interlocked.Exchange(ref currentTournamentId, tournamentId);
            }
        }

        private readonly ITournamentDisconnectHandler disconnectHandler;

        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, ITournamentCallback>> subscribersByTournamentId =
            new ConcurrentDictionary<long, ConcurrentDictionary<string, ITournamentCallback>>();

        private readonly ConcurrentDictionary<string, SessionEntry> sessionEntries =
            new ConcurrentDictionary<string, SessionEntry>(StringComparer.Ordinal);

        public TournamentSubscriptionStore(ITournamentDisconnectHandler disconnectHandler)
        {
            this.disconnectHandler = disconnectHandler ?? throw new ArgumentNullException(nameof(disconnectHandler));
        }

        public bool Subscribe(long tournamentId, long userId, ITournamentCallback callbackChannel)
        {
            if (tournamentId <= NO_TOURNAMENT_ID || userId <= INVALID_ID || callbackChannel == null)
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

            SessionEntry sessionEntry = EnsureSessionEntry(sessionId, channelObject, userId, tournamentId);

            long previousTournamentId = sessionEntry.SetTournamentId(tournamentId);

            if (previousTournamentId > NO_TOURNAMENT_ID && previousTournamentId != tournamentId)
            {
                RemoveSubscriberFromTournament(previousTournamentId, sessionId);
            }

            ConcurrentDictionary<string, ITournamentCallback> subscribersForTournament = subscribersByTournamentId.GetOrAdd(
                tournamentId,
                _ => new ConcurrentDictionary<string, ITournamentCallback>(StringComparer.Ordinal));

            subscribersForTournament[sessionId] = callbackChannel;

            return true;
        }

        public void Unsubscribe(long tournamentId, ITournamentCallback callbackChannel)
        {
            if (tournamentId <= NO_TOURNAMENT_ID || callbackChannel == null)
            {
                return;
            }

            string sessionId = TryGetSessionId(callbackChannel);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            RemoveSubscriberFromTournament(tournamentId, sessionId);

            if (sessionEntries.TryGetValue(sessionId, out SessionEntry sessionEntry))
            {
                if (sessionEntry.TournamentId == tournamentId)
                {
                    sessionEntry.SetTournamentId(NO_TOURNAMENT_ID);
                }
            }
        }

        public IReadOnlyList<ITournamentCallback> GetSubscribers(long tournamentId)
        {
            if (tournamentId <= NO_TOURNAMENT_ID)
            {
                return Array.Empty<ITournamentCallback>();
            }

            if (!subscribersByTournamentId.TryGetValue(tournamentId, out ConcurrentDictionary<string, ITournamentCallback> subscribersForTournament))
            {
                return Array.Empty<ITournamentCallback>();
            }

            return subscribersForTournament.Values.ToList();
        }

        private SessionEntry EnsureSessionEntry(string sessionId, ICommunicationObject channelObject, long userId, long tournamentId)
        {
            return sessionEntries.GetOrAdd(
                sessionId,
                _ =>
                {
                    var entry = new SessionEntry(channelObject, userId, tournamentId);
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
                long lastKnownTournamentId = removedEntry.TournamentId;

                if (lastKnownTournamentId > NO_TOURNAMENT_ID)
                {
                    RemoveSubscriberFromTournament(lastKnownTournamentId, sessionId);
                }
                else
                {
                    RemoveSubscriberFromAllTournaments(sessionId);
                }

                TryHandleDisconnect(removedEntry.UserId);
                return;
            }
            RemoveSubscriberFromAllTournaments(sessionId);
        }

        private void RemoveSubscriberFromTournament(long tournamentId, string sessionId)
        {
            if (tournamentId <= NO_TOURNAMENT_ID || string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            if (subscribersByTournamentId.TryGetValue(tournamentId, out ConcurrentDictionary<string, ITournamentCallback> subscribersForTournament))
            {
                subscribersForTournament.TryRemove(sessionId, out _);

                if (subscribersForTournament.IsEmpty)
                {
                    subscribersByTournamentId.TryRemove(tournamentId, out _);
                }
            }
        }

        private void RemoveSubscriberFromAllTournaments(string sessionId)
        {
            foreach (KeyValuePair<long, ConcurrentDictionary<string, ITournamentCallback>> kvp in subscribersByTournamentId)
            {
                kvp.Value.TryRemove(sessionId, out _);

                if (kvp.Value.IsEmpty)
                {
                    subscribersByTournamentId.TryRemove(kvp.Key, out _);
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
                disconnectHandler.HandleTournamentDisconnect(userId, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Logger.Error("TournamentSubscriptionStore.HandleDisconnect failed.", ex);
            }
        }

        private static string TryGetSessionId(ITournamentCallback callbackChannel)
        {
            return (callbackChannel as IContextChannel)?.SessionId ?? string.Empty;
        }

        public sealed class TournamentDisconnectHandler : ITournamentDisconnectHandler
        {
            private readonly IGuessWhoUnitOfWorkFactory _unitOfWorkFactory;
            private readonly ILog _logger;

            public TournamentDisconnectHandler(IGuessWhoUnitOfWorkFactory unitOfWorkFactory, ILog logger)
            {
                _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public void HandleTournamentDisconnect(long userId, DateTime nowUtc)
            {
                if (userId <= INVALID_ID)
                {
                    return;
                }

                try
                {
                    using IGuessWhoUnitOfWork unitOfWork = _unitOfWorkFactory.Create();
                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    bool success = unitOfWork.Tournaments.HandleDisconnect(userId, nowUtc);

                    if (!success)
                    {
                        transaction.Rollback();
                        return;
                    }

                    unitOfWork.Flush();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _logger.Error("TournamentDisconnectHandler.HandleTournamentDisconnect failed.", ex);
                }
            }
        }
    }
}