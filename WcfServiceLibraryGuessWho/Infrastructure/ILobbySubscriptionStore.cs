using GuessWhoContracts.Services;
using System.Collections.Generic;

namespace GuessWhoServices.Infrastructure
{
    public interface ILobbySubscriptionStore
    {
        bool Subscribe(long matchId, IMatchCallback callbackChannel);
        void Unsubscribe(long matchId, IMatchCallback callbackChannel);
        IReadOnlyList<IMatchCallback> GetSubscribers(long matchId);
    }
}