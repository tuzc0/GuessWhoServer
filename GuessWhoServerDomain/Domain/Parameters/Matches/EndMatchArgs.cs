namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class EndMatchArgs
    {
        public long MatchId { get; set; }

        public long WinnerUserId { get; set; }
    }
}
