using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Models.Match
{
    public sealed class MatchDeckRecord
    {
        private const long INVALID_MATCH_ID = 0; 

        public long MatchId { get; init; }
        public IReadOnlyList<string> CharacterDeckIds { get; init; } = Array.Empty<string>();

        public bool IsValid => MatchId > INVALID_MATCH_ID && CharacterDeckIds.Count > 0;

        public MatchDeckRecord(long matchId, IReadOnlyList<string> characterIds)
        {
            MatchId = matchId;
            CharacterDeckIds = characterIds ?? Array.Empty<string>();
        }

        public static MatchDeckRecord CreateInvalid()
        {
            return new MatchDeckRecord(
                matchId: 0, 
                characterIds: Array.Empty<string>()
            );
        }
    }
}
