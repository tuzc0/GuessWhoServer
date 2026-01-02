using GuessWhoContracts.Services;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Infrastructure;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Coordinators.Match
{
    public sealed class LobbySubscriptionOperations : ILobbySubscriptionOperations
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbySubscriptionOperations));

        private const string CONTEXT_SUBSCRIBE = nameof(LobbySubscriptionOperations) + "." + nameof(Subscribe);
        private const string CONTEXT_UNSUBSCRIBE = nameof(LobbySubscriptionOperations) + "." + nameof(Unsubscribe);

        private const long INVALID_ID = 0;

        private readonly ILobbySubscriptionStore lobbySubscriptionStore;

        public LobbySubscriptionOperations(ILobbySubscriptionStore lobbySubscriptionStore)
        {
            this.lobbySubscriptionStore = lobbySubscriptionStore ??
                throw new ArgumentNullException(nameof(lobbySubscriptionStore));
        }

        public void Subscribe(LobbySubscriptionArgs lobbySubscriptionArgs)
        {
            if (lobbySubscriptionArgs == null)
            {
                Logger.WarnFormat("{0}: args is null.", CONTEXT_SUBSCRIBE);
                return;
            }

            long matchId = lobbySubscriptionArgs.MatchId;
            long userId = lobbySubscriptionArgs.UserId;

            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                Logger.WarnFormat("{0}: invalid ids. MatchId={1}, UserId={2}.", CONTEXT_SUBSCRIBE, matchId, userId);
                return;
            }

            OperationContext operationContext = OperationContext.Current;
            if (operationContext == null)
            {
                Logger.WarnFormat("{0}: OperationContext.Current is null.", CONTEXT_SUBSCRIBE);
                return;
            }

            IMatchCallback callbackChannel = operationContext.GetCallbackChannel<IMatchCallback>();
            if (callbackChannel == null)
            {
                Logger.WarnFormat("{0}: callback channel is null. MatchId={1}, UserId={2}.", CONTEXT_SUBSCRIBE, matchId, userId);
                return;
            }

            bool subscribed = lobbySubscriptionStore.Subscribe(matchId, userId, callbackChannel);

            if (!subscribed)
            {
                Logger.WarnFormat("{0}: subscribe failed. MatchId={1}, UserId={2}.", CONTEXT_SUBSCRIBE, matchId, userId);
            }
        }

        public void Unsubscribe(LobbySubscriptionArgs lobbySubscriptionArgs)
        {
            if (lobbySubscriptionArgs == null)
            {
                Logger.WarnFormat("{0}: args is null.", CONTEXT_UNSUBSCRIBE);
                return;
            }

            long matchId = lobbySubscriptionArgs.MatchId;

            if (matchId <= INVALID_ID)
            {
                Logger.WarnFormat("{0}: invalid matchId. MatchId={1}.", CONTEXT_UNSUBSCRIBE, matchId);
                return;
            }

            OperationContext operationContext = OperationContext.Current;
            if (operationContext == null)
            {
                Logger.WarnFormat("{0}: OperationContext.Current is null.", CONTEXT_UNSUBSCRIBE);
                return;
            }

            IMatchCallback callbackChannel = operationContext.GetCallbackChannel<IMatchCallback>();
            if (callbackChannel == null)
            {
                Logger.WarnFormat("{0}: callback channel is null. MatchId={1}.", CONTEXT_UNSUBSCRIBE, matchId);
                return;
            }

            lobbySubscriptionStore.Unsubscribe(matchId, callbackChannel);
        }
    }
}
