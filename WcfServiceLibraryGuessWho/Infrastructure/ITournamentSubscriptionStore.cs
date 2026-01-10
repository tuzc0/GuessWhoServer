using GuessWhoContracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoServices.Infrastructure
{
    public interface ITournamentSubscriptionStore
    {
        bool Subscribe(long tournamentId, long userId, ITournamentCallback callback);
        void Unsubscribe(long tournamentId, ITournamentCallback callback);
        IReadOnlyList<ITournamentCallback> GetSubscribers(long tournamentId);
    }
}
