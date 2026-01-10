using GuessWhoServerDomain.Domain.Models.Leaderboard;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Results.Leaderboard
{
    public class LeaderboardResult
    {
        public IList<LeaderboardPlayer> TopPlayers { get; set; }
        public LeaderboardPlayer CurrentUserStats { get; set; }

        public LeaderboardResult()
        {
            TopPlayers = new List<LeaderboardPlayer>();
        }
    }
}