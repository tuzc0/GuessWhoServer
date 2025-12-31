namespace GuessWhoCore.Contracts.Requests
{
    public class GetOrCreateMatchDeckRequest
    {
        public long MatchId { get; set; }
        public byte ModeId { get; set; }
    }
}
