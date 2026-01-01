using System;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class PassTurnArgs
    {
        public long MatchId { get; set; }
        public long UserId { get; set; }
        public byte[] ExceptedRowVersion { get; set; }
        public DateTime NowUtc { get; set; }
    }
}
