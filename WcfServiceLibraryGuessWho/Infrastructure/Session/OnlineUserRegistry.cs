using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace GuessWhoServices.Infrastructure.Session
{
    public sealed class OnlineUserRegistry : IOnlineUserRegistry, IDisposable
    {
        private sealed class OnlineSession
        {
            public OnlineSession(long userId, string sessionId, IContextChannel channel, DateTime expiresAtUtc)
            {
                UserId = userId;
                SessionId = sessionId ?? string.Empty;
                Channel = channel ?? throw new ArgumentNullException(nameof(channel));
                ExpiresAtUtc = expiresAtUtc;
            }

            public long UserId { get; }

            public string SessionId { get; }

            public IContextChannel Channel { get; }

            public DateTime ExpiresAtUtc { get; set; }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(OnlineUserRegistry));

        private const string LOG_CTX_REGISTER = "OnlineUserRegistry.RegisterOrReplace";
        private const string LOG_CTX_TOUCH = "OnlineUserRegistry.Touch";
        private const string LOG_CTX_REMOVE = "OnlineUserRegistry.Remove";

        private const int MIN_VALID_ID = 1;
        private const string SESSION_ID_FORMAT_NO_HYPHENS = "N";

        private readonly OnlineRegistrySettings settings;

        private readonly ConcurrentDictionary<long, OnlineSession> sessionsByUserId =
            new ConcurrentDictionary<long, OnlineSession>();

        private readonly ConcurrentDictionary<long, object> locksByUserId =
            new ConcurrentDictionary<long, object>();

        private readonly Timer cleanupTimer;

        private bool isDisposed;

        public OnlineUserRegistry(OnlineRegistrySettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (settings.LeaseSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.LeaseSeconds));
            }

            if (settings.CleanupIntervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.CleanupIntervalSeconds));
            }

            TimeSpan cleanupPeriod = TimeSpan.FromSeconds(settings.CleanupIntervalSeconds);

            cleanupTimer = new Timer(
                _ => CleanupExpiredOrDeadSessions(),
                null,
                cleanupPeriod,
                cleanupPeriod);
        }

        public RegisterOrReplaceResult RegisterOrReplace(RegisterOrReplaceRequest request)
        {
            EnsureNotDisposed();
            EnsureValidUserIdOrThrow(request?.UserId ?? 0);

            if (request.Channel == null)
            {
                throw new ArgumentNullException(nameof(request.Channel));
            }

            DateTime nowUtc = DateTime.UtcNow;
            DateTime newExpiresAtUtc = nowUtc.AddSeconds(settings.LeaseSeconds);

            object gate = locksByUserId.GetOrAdd(request.UserId, _ => new object());

            lock (gate)
            {
                bool hasZombieCleaned = false;

                if (sessionsByUserId.TryGetValue(request.UserId, out OnlineSession existing))
                {
                    bool isExistingAlive = IsSessionAlive(existing, nowUtc);

                    if (isExistingAlive)
                    {
                        Logger.InfoFormat(
                            "{0}: reject login (account in use). userId='{1}', sessionId='{2}'.",
                            LOG_CTX_REGISTER,
                            request.UserId,
                            existing.SessionId);

                        return RegisterOrReplaceResult.RejectedAccountInUse();
                    }

                    hasZombieCleaned = RemoveInternal(existing.UserId, existing.Channel, "zombie-clean", nowUtc);
                }

                string sessionId = Guid.NewGuid().ToString(SESSION_ID_FORMAT_NO_HYPHENS);
                var session = new OnlineSession(request.UserId, sessionId, request.Channel, newExpiresAtUtc);

                HookChannel(session);

                sessionsByUserId[request.UserId] = session;

                Logger.InfoFormat(
                    "{0}: registered. userId='{1}', sessionId='{2}', expiresAtUtc='{3:O}', zombieCleaned='{4}'.",
                    LOG_CTX_REGISTER,
                    request.UserId,
                    sessionId,
                    newExpiresAtUtc,
                    hasZombieCleaned);

                return RegisterOrReplaceResult.Registered(sessionId, hasZombieCleaned);
            }
        }

        public bool IsOnline(IsOnlineRequest request)
        {
            EnsureNotDisposed();
            EnsureValidUserIdOrThrow(request?.UserId ?? 0);

            DateTime nowUtc = DateTime.UtcNow;

            if (!sessionsByUserId.TryGetValue(request.UserId, out OnlineSession session))
            {
                return false;
            }

            if (IsSessionAlive(session, nowUtc))
            {
                return true;
            }

            RemoveInternal(request.UserId, session.Channel, "is-online-clean", nowUtc);
            return false;
        }

        public IReadOnlyCollection<long> GetOnlineUserIds()
        {
            EnsureNotDisposed();

            DateTime nowUtc = DateTime.UtcNow;

            List<long> onlineIds = new List<long>();

            foreach (KeyValuePair<long, OnlineSession> entry in sessionsByUserId)
            {
                OnlineSession session = entry.Value;

                if (session == null)
                {
                    continue;
                }

                if (IsSessionAlive(session, nowUtc))
                {
                    onlineIds.Add(entry.Key);
                }
                else
                {
                    RemoveInternal(entry.Key, session.Channel, "get-online-clean", nowUtc);
                }
            }

            return onlineIds.AsReadOnly();
        }

        public void Touch(TouchRequest request)
        {
            EnsureNotDisposed();
            EnsureValidUserIdOrThrow(request?.UserId ?? 0);

            DateTime nowUtc = DateTime.UtcNow;

            if (!sessionsByUserId.TryGetValue(request.UserId, out OnlineSession session))
            {
                return;
            }

            if (!IsChannelOpened(session.Channel))
            {
                RemoveInternal(request.UserId, session.Channel, "touch-channel-dead", nowUtc);
                return;
            }

            session.ExpiresAtUtc = nowUtc.AddSeconds(settings.LeaseSeconds);

            Logger.InfoFormat(
                "{0}: touched. userId='{1}', sessionId='{2}', expiresAtUtc='{3:O}'.",
                LOG_CTX_TOUCH,
                request.UserId,
                session.SessionId,
                session.ExpiresAtUtc);
        }

        public void Remove(RemoveRequest request)
        {
            EnsureNotDisposed();
            EnsureValidUserIdOrThrow(request?.UserId ?? 0);

            if (request.Channel == null)
            {
                throw new ArgumentNullException(nameof(request.Channel));
            }

            DateTime nowUtc = DateTime.UtcNow;

            RemoveInternal(request.UserId, request.Channel, "explicit-remove", nowUtc);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            cleanupTimer.Dispose();

            sessionsByUserId.Clear();
            locksByUserId.Clear();
        }

        private void CleanupExpiredOrDeadSessions()
        {
            if (isDisposed)
            {
                return;
            }

            DateTime nowUtc = DateTime.UtcNow;

            foreach (KeyValuePair<long, OnlineSession> entry in sessionsByUserId)
            {
                OnlineSession session = entry.Value;

                if (session == null)
                {
                    continue;
                }

                bool isAlive = IsSessionAlive(session, nowUtc);

                if (!isAlive)
                {
                    RemoveInternal(entry.Key, session.Channel, "timer-clean", nowUtc);
                }
            }
        }

        private void HookChannel(OnlineSession session)
        {
            IContextChannel channel = session.Channel;

            channel.Faulted += (_, __) =>
            {
                DateTime nowUtc = DateTime.UtcNow;
                RemoveInternal(session.UserId, channel, "channel-faulted", nowUtc);
            };

            channel.Closed += (_, __) =>
            {
                DateTime nowUtc = DateTime.UtcNow;
                RemoveInternal(session.UserId, channel, "channel-closed", nowUtc);
            };
        }

        private bool RemoveInternal(long userId, IContextChannel channel, string reason, DateTime nowUtc)
        {
            if (!sessionsByUserId.TryGetValue(userId, out OnlineSession current))
            {
                return false;
            }

            if (current == null)
            {
                sessionsByUserId.TryRemove(userId, out _);
                return true;
            }

            if (!ReferenceEquals(current.Channel, channel))
            {
                return false;
            }

            bool removed = sessionsByUserId.TryRemove(userId, out _);

            if (removed)
            {
                Logger.InfoFormat(
                    "{0}: removed. userId='{1}', sessionId='{2}', reason='{3}', nowUtc='{4:O}'.",
                    LOG_CTX_REMOVE,
                    userId,
                    current.SessionId,
                    reason ?? string.Empty,
                    nowUtc);
            }

            return removed;
        }

        private bool IsSessionAlive(OnlineSession session, DateTime nowUtc)
        {
            if (session == null)
            {
                return false;
            }

            if (session.ExpiresAtUtc <= nowUtc)
            {
                return false;
            }

            return IsChannelOpened(session.Channel);
        }

        private static bool IsChannelOpened(IContextChannel channel)
        {
            if (channel == null)
            {
                return false;
            }

            try
            {
                return channel.State == CommunicationState.Opened;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private void EnsureNotDisposed()
        {
            if (!isDisposed)
            {
                return;
            }

            throw new ObjectDisposedException(nameof(OnlineUserRegistry));
        }

        private static void EnsureValidUserIdOrThrow(long userId)
        {
            if (userId >= MIN_VALID_ID)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(userId));
        }
    }
}
