using GuessWhoContracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoServices.Infrastructure
{
    public interface ITournamentCallbackDispatcher
    {
        void Broadcast(long tournamentId, Action<ITournamentCallback> action);
    }
}
