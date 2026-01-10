using GuessWhoServerDomain.Domain.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface ITournamentRepository
    {
        long CreateTournament(long hostUserId, int turnSeconds);
        bool AddPlayerToTournament(long tournamentId, long userId);
        IEnumerable<TournamentPlayer> GetTournamentPlayers(long tournamentId);
        void LinkMatchToTournament(long tournamentId, long matchId, bool isFinal);
        void UpdateStatus(long tournamentId, int statusId);
        bool HandleDisconnect(long userId, DateTime nowUtc);
    }
}
