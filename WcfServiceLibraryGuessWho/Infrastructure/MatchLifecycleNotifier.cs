using log4net;
using System;

namespace GuessWhoServices.Infrastructure
{
    public sealed class MatchLifecycleNotifier : IMatchLifecycleNotifier
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchLifecycleNotifier));

        private const string CONTEXT_STARTED = "MatchLifecycleNotifier.NotifyMatchStarted";
        private const string CONTEXT_ENDED = "MatchLifecycleNotifier.NotifyMatchEnded";
        private const string CONTEXT_SECRET = "MatchLifecycleNotifier.NotifySecretCharacterChosen";
        private const string CONTEXT_ALL_SECRETS = "MatchLifecycleNotifier.NotifyAllSecretCharactersChosen";

        private readonly IMatchCallbackDispatcher callbackDispatcher;

        public MatchLifecycleNotifier(IMatchCallbackDispatcher callbackDispatcher)
        {
            this.callbackDispatcher = callbackDispatcher ?? throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public void NotifyMatchStarted(long matchId)
        {
            if (matchId <= 0)
            {
                Logger.WarnFormat("{0}: invalid matchId={1}.", CONTEXT_STARTED, matchId);
                return;
            }

            callbackDispatcher.Broadcast(matchId, callback => callback.OnGameStarted(matchId));
        }

        public void NotifyMatchEnded(long matchId, long winnerUserId)
        {
            if (matchId <= 0 || winnerUserId <= 0)
            {
                Logger.WarnFormat("{0}: invalid inputs. matchId={1} winnerUserId={2}.", CONTEXT_ENDED, matchId, winnerUserId);
                return;
            }

            callbackDispatcher.Broadcast(matchId, callback => callback.OnGameEnded(matchId, winnerUserId));
        }

        public void NotifySecretCharacterChosen(long matchId, long userId)
        {
            if (matchId <= 0 || userId <= 0)
            {
                Logger.WarnFormat("{0}: invalid inputs. matchId={1} userId={2}.", CONTEXT_SECRET, matchId, userId);
                return;
            }

            callbackDispatcher.Broadcast(matchId, callback => callback.OnSecretCharacterChosen(matchId, userId));
        }

        public void NotifyAllSecretCharactersChosen(long matchId)
        {
            if (matchId <= 0)
            {
                Logger.WarnFormat("{0}: invalid matchId={1}.", CONTEXT_ALL_SECRETS, matchId);
                return;
            }

            callbackDispatcher.Broadcast(matchId, callback => callback.OnAllSecretCharactersChosen(matchId));
        }
    }
}
