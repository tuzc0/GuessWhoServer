using System;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class AdvanceTurnArgs
    {
        public long MatchId { get; set; }
        public long UserId { get; set; }
        public byte[] ExpectedRowVersion { get; set; }
        public DateTime NowUtc { get; set; }
    }
}
