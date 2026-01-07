namespace GuessWhoContracts.Dtos.Dto
{
    public class LeaderboardPlayerDto
    {
        public int Rank { get; set; }
        public string DisplayName { get; set; }
        public string AvatarId { get; set; }
        public int Wins { get; set; }
    }
}