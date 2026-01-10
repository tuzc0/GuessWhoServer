using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Response;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Infrastructure;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace GuessWhoServices.Coordinators.Tournament
{
    public sealed class TournamentSubscriptionOperations : ITournamentSubscriptionOperations
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TournamentSubscriptionOperations));

        private readonly ITournamentSubscriptionStore tournamentSubscriptionStore;
        private readonly ITournamentCallbackDispatcher tournamentCallbackDispatcher;

        public TournamentSubscriptionOperations(
            ITournamentSubscriptionStore tournamentSubscriptionStore,
            ITournamentCallbackDispatcher tournamentCallbackDispatcher)
        {
            this.tournamentSubscriptionStore = tournamentSubscriptionStore ??
                throw new ArgumentNullException(nameof(tournamentSubscriptionStore));

            this.tournamentCallbackDispatcher = tournamentCallbackDispatcher ??
                throw new ArgumentNullException(nameof(tournamentCallbackDispatcher));
        }

        public bool Subscribe(TournamentSubscriptionArgs args)
        {
            if (args == null) return false;

            ITournamentCallback callbackChannel = OperationContext.Current?.GetCallbackChannel<ITournamentCallback>();
            if (callbackChannel == null) return false;

            return tournamentSubscriptionStore.Subscribe(args.TournamentId, args.UserId, callbackChannel);
        }

        public bool Unsubscribe(TournamentSubscriptionArgs args)
        {
            if (args == null) return false;

            ITournamentCallback callbackChannel = OperationContext.Current?.GetCallbackChannel<ITournamentCallback>();
            if (callbackChannel == null) return false;

            tournamentSubscriptionStore.Unsubscribe(args.TournamentId, callbackChannel);
            return true;
        }

        public void NotifyTournamentLobbyUpdated(long tournamentId, IEnumerable<TournamentPlayerDto> players)
        {
            tournamentCallbackDispatcher.Broadcast(tournamentId, callback => callback.OnTournamentLobbyUpdated(players));
        }

        public void NotifyTournamentStarted(long tournamentId, long match1Id, long match2Id)
        {
            tournamentCallbackDispatcher.Broadcast(tournamentId, callback => callback.OnTournamentStarted(match1Id, match2Id));
        }

        public void NotifyTournamentFinalStarted(long tournamentId, long finalMatchId)
        {
            tournamentCallbackDispatcher.Broadcast(tournamentId, callback => callback.OnTournamentFinalStarted(finalMatchId));
        }
    }
}