using log4net;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;

namespace GuessWhoServices.Session
{
    internal static class ActiveSessionRegistry
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ActiveSessionRegistry));

        private const long INVALID_ID = 0;
        private static readonly Guid EMPTY_SESSION_ID = Guid.Empty;

        private const string LOG_CTX_ABORT = "ActiveSessionRegistry.AbortChannelSafely";
        private const string LOG_CTX_ATTACH_REJECTED = "ActiveSessionRegistry.AttachChannelRejected";

        private static readonly ConcurrentDictionary<long, ActiveSession> SessionsByUserId =
            new ConcurrentDictionary<long, ActiveSession>();

        internal static Guid StartOrReplace(long userId)
        {
            if (userId <= INVALID_ID)
            {
                return EMPTY_SESSION_ID;
            }

            Guid newSessionId = Guid.NewGuid();

            SessionsByUserId.AddOrUpdate(
                userId,
                _ => ActiveSession.Create(newSessionId, channel: null),
                (_, existing) =>
                {
                    AbortChannelSafely(existing.Channel);
                    return ActiveSession.Create(newSessionId, channel: null);
                });

            return newSessionId;
        }

        internal static bool IsValid(long userId, Guid sessionId)
        {
            if (userId <= INVALID_ID || sessionId == EMPTY_SESSION_ID)
            {
                return false;
            }

            if (!SessionsByUserId.TryGetValue(userId, out ActiveSession current))
            {
                return false;
            }

            return current.SessionId == sessionId;
        }

        internal static void AttachChannel(long userId, Guid sessionId, IContextChannel channel)
        {
            if (userId <= INVALID_ID || sessionId == EMPTY_SESSION_ID || channel == null)
            {
                return;
            }

            SessionsByUserId.AddOrUpdate(
                userId,
                _ => ActiveSession.Create(sessionId, channel),
                (_, existing) =>
                {
                    if (existing.SessionId != sessionId)
                    {
                        Logger.WarnFormat("{0}: rejected channel attach. userId={1}.", LOG_CTX_ATTACH_REJECTED, userId);
                        AbortChannelSafely(channel);
                        return existing;
                    }

                    return ActiveSession.Create(sessionId, channel);
                });
        }

        internal static void EndAll(long userId)
        {
            if (userId <= INVALID_ID)
            {
                return;
            }

            if (SessionsByUserId.TryRemove(userId, out ActiveSession existing))
            {
                AbortChannelSafely(existing.Channel);
            }
        }

        private static void AbortChannelSafely(IContextChannel channel)
        {
            if (channel == null)
            {
                return;
            }

            try
            {
                ((ICommunicationObject)channel).Abort();
            }
            catch (CommunicationObjectAbortedException ex)
            {
                Logger.Debug(LOG_CTX_ABORT, ex);
            }
            catch (CommunicationObjectFaultedException ex)
            {
                Logger.Debug(LOG_CTX_ABORT, ex);
            }
            catch (CommunicationException ex)
            {
                Logger.Debug(LOG_CTX_ABORT, ex);
            }
            catch (TimeoutException ex)
            {
                Logger.Debug(LOG_CTX_ABORT, ex);
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Debug(LOG_CTX_ABORT, ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug(LOG_CTX_ABORT, ex);
            }
        }

        private readonly struct ActiveSession
        {
            public Guid SessionId { get; }
            public IContextChannel Channel { get; }

            private ActiveSession(Guid sessionId, IContextChannel channel)
            {
                SessionId = sessionId;
                Channel = channel;
            }

            public static ActiveSession Create(Guid sessionId, IContextChannel channel)
            {
                return new ActiveSession(sessionId, channel);
            }
        }
    }
}
