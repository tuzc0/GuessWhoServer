using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class SaveMatchDeckArgs
    {
        public long MatchId { get; set; }
        public IReadOnlyList<string> CharacterIds { get; set; } = Array.Empty<string>();
    }
}
