using GuessWhoServerDomain.Domain.Results.Leaderboard;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface ILeaderboardManager
    {
        LeaderboardResult GetGlobalLeaderboard(int topN, long requestingUserId);
    }
}
