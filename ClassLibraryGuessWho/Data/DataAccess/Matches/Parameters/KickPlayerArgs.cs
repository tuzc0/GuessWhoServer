namespace ClassLibraryGuessWho.Data.DataAccess.Match.Parameters
{
    public sealed class KickPlayerArgs
    {
        public long MatchId { get; set; }
        public long RequesterUserId { get; set; }
        public long TargetUserId { get; set; }
    }
}
