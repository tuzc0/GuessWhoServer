using System;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class ClaimTimeoutArgs
    {
        public long MatchId { get; init; }
        public long UserId { get; init; }
        public DateTime NowUtc { get; init; }
    }
}
