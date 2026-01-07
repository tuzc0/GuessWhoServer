namespace GuessWhoServerDomain.Domain.Models.Leaderboard
{
    public class LeaderboardPlayer
    {
        public int Rank { get; set; }
        public string DisplayName { get; set; }
        public string AvatarId { get; set; }
        public int Wins { get; set; }
    }
}