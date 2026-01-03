using GuessWhoContracts.Services;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Coordinators.Match;
using GuessWhoServices.Infrastructure;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Coordinators
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

        public bool Subscribe(LobbySubscriptionArgs lobbySubscriptionArgs)
        {
            if (lobbySubscriptionArgs == null)
            {
                Logger.WarnFormat("{0}: args is null.", CONTEXT_SUBSCRIBE);
                return false;
            }

            long matchId = lobbySubscriptionArgs.MatchId;
            long userId = lobbySubscriptionArgs.UserId;

            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                Logger.WarnFormat("{0}: invalid ids. MatchId={1}, UserId={2}.", CONTEXT_SUBSCRIBE, matchId, userId);
                return false;
            }

            IMatchCallback callbackChannel = OperationContext.Current?.GetCallbackChannel<IMatchCallback>();

            if (callbackChannel == null)
            {
                Logger.WarnFormat("{0}: OperationContext.Current is null.", CONTEXT_SUBSCRIBE);
                return false;
            }

            return lobbySubscriptionStore.Subscribe(matchId, userId, callbackChannel);
        }

        public bool Unsubscribe(LobbySubscriptionArgs lobbySubscriptionArgs)
        {
            if (lobbySubscriptionArgs == null)
            {
                Logger.WarnFormat("{0}: args is null.", CONTEXT_UNSUBSCRIBE);
                return false;
            }

            long matchId = lobbySubscriptionArgs.MatchId;
            long userId = lobbySubscriptionArgs.UserId;

            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                Logger.WarnFormat("{0}: invalid matchId. MatchId={1}.", CONTEXT_UNSUBSCRIBE, matchId);
                return false;
            }

            IMatchCallback callbackChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
            
            if (callbackChannel == null)
            {
                Logger.WarnFormat("{0}: callback channel is null. MatchId={1}.", CONTEXT_UNSUBSCRIBE, matchId);
                return false;
            }

            lobbySubscriptionStore.Unsubscribe(matchId, callbackChannel);

            return true;
        }
    }
}
