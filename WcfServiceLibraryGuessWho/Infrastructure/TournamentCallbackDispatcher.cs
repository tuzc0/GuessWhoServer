using GuessWhoContracts.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace GuessWhoServices.Infrastructure
{
    public sealed class TournamentCallbackDispatcher : ITournamentCallbackDispatcher
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TournamentCallbackDispatcher));
        private readonly ITournamentSubscriptionStore subscriptionStore;

        public TournamentCallbackDispatcher(ITournamentSubscriptionStore subscriptionStore)
        {
            this.subscriptionStore = subscriptionStore ?? throw new ArgumentNullException(nameof(subscriptionStore));
        }

        public void Broadcast(long tournamentId, Action<ITournamentCallback> action)
        {
            IReadOnlyList<ITournamentCallback> subscribers = subscriptionStore.GetSubscribers(tournamentId);

            foreach (var subscriber in subscribers)
            {
                try
                {
                    // Ejecuta la acción (ej: OnTournamentStarted) sobre el canal WCF del cliente
                    action(subscriber);
                }
                catch (Exception ex) when (ex is TimeoutException || ex is CommunicationException || ex is ObjectDisposedException)
                {
                    Logger.WarnFormat("Callback failed for tournament {0}. Subscriber communication lost.", tournamentId);
                    // No eliminamos aquí directamente para evitar modificar la colección mientras se recorre, 
                    // el sistema de auto-cleanup del Store se encargará por los eventos Faulted/Closed.
                }
                catch (Exception ex)
                {
                    Logger.Error($"Unexpected error broadcasting to tournament {tournamentId}", ex);
                }
            }
        }
    }
}