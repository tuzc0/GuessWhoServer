namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class KickPlayerArgs
    {
        public long MatchId { get; set; }
        public long UserProfileId { get; set; }
        public long RequesterUserId { get; set; }
        public long TargetUserId { get; set; }
    }
}
