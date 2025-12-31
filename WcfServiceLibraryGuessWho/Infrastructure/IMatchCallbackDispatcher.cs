using GuessWhoContracts.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace GuessWhoServices.Infrastructure
{
    public interface IMatchCallbackDispatcher
    {
        void Broadcast(long matchId, Action<IMatchCallback> action);
    }

    public sealed class MatchCallbackDispatcher : IMatchCallbackDispatcher
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchCallbackDispatcher));

        private const string REASON_TIMEOUT = "Timeout";
        private const string REASON_COMMUNICATION = "CommunicationError";
        private const string REASON_DISPOSED = "ObjectDisposed";
        private const string LOG_CALLBACK_FAILED = "Callback failed. reason={0} matchId={1}. Removing subscriber.";
        private const string LOG_CALLBACK_UNEXPECTED = "Unexpected error broadcasting callback. matchId={0}.";

        private readonly ILobbySubscriptionStore subscriptionStore;

        public MatchCallbackDispatcher(ILobbySubscriptionStore subscriptionStore)
        {
            this.subscriptionStore = subscriptionStore ?? 
                throw new ArgumentNullException(nameof(subscriptionStore));
        }

        public void Broadcast(long matchId, Action<IMatchCallback> action)
        {
            if (matchId <= 0 || action == null)
            {
                return;
            }

            IReadOnlyList<IMatchCallback> subscribers = subscriptionStore.GetSubscribers(matchId);

            foreach (IMatchCallback subscriber in subscribers)
            {
                try
                {
                    action(subscriber);
                }
                catch (TimeoutException ex)
                {
                    LogAndRemove(matchId, subscriber, REASON_TIMEOUT, ex);
                }
                catch (CommunicationException ex)
                {
                    LogAndRemove(matchId, subscriber, REASON_COMMUNICATION, ex);
                }
                catch (ObjectDisposedException ex)
                {
                    LogAndRemove(matchId, subscriber, REASON_DISPOSED, ex);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat(LOG_CALLBACK_UNEXPECTED, matchId);
                    Logger.Debug("Callback exception (unexpected).", ex);
                }
            }
        }

        private void LogAndRemove(long matchId, IMatchCallback subscriber, string reason, Exception ex)
        {
            Logger.WarnFormat(LOG_CALLBACK_FAILED, reason, matchId);
            Logger.Debug("Callback exception details.", ex);

            subscriptionStore.Unsubscribe(matchId, subscriber);
        }
    }
}
