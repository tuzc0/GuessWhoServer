using GuessWhoServerDomain.Domain.Models.Leaderboard;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface ILeaderboardRepository
    {
        IList<LeaderboardPlayer> GetTopWinners(int limit);
        LeaderboardPlayer GetPlayerStats(long userId);
    }
}