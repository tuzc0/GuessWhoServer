using GuessWhoServerDomain.Domain.Enums.Matches;
using System;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public sealed class MatchDeckResult
    {
        private const long INVALID_MATCH_ID = 0;

        public MatchDeckResultCode Code { get; init; }

        public long MatchId { get; init; }

        public IReadOnlyList<string> CharacterDeckIds { get; init; } = Array.Empty<string>();

        public bool IsSuccess => Code == MatchDeckResultCode.Success;

        public bool IsValid => IsSuccess && MatchId > INVALID_MATCH_ID && CharacterDeckIds.Count > 0;

        public static MatchDeckResult Success(long matchId, IReadOnlyList<string> characterIds)
        {
            return new MatchDeckResult
            {
                Code = MatchDeckResultCode.Success,
                MatchId = matchId,
                CharacterDeckIds = characterIds ?? Array.Empty<string>()
            };
        }

        public static MatchDeckResult Fail(MatchDeckResultCode code)
        {
            return new MatchDeckResult
            {
                Code = code,
                MatchId = INVALID_MATCH_ID,
                CharacterDeckIds = Array.Empty<string>()
            };
        }
    }
}
