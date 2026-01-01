using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class InitializeTurnOrderArgs
    {
        public long MatchId { get; set; }
        public IReadOnlyList<long> UserIdsInOrder { get; set; }
        public DateTime NowUtc { get; set; }
    }
}
