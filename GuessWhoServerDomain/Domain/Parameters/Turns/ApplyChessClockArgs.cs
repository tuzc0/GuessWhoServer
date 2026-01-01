using System;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class ApplyChessClockArgs
    {
        public long MatchId { get; set; }
        public long UserId { get; set; }
        public DateTime NowUtc { get; set; }
    }
}
