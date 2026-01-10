using System;

namespace GuessWhoServerDomain.Domain.Models.Matches
{
    public readonly record struct MatchSnapshot(long MatchId, string MatchCode, byte StatusId, byte VisibilityId, byte ModeId, DateTime CreatedAtUtc)
    {
        public const long INVALID_ID_MATCH_ID = 0;

        public bool IsValid => MatchId > INVALID_ID_MATCH_ID;

        public static MatchSnapshot CreateInvalid()
        {
            return new MatchSnapshot(
                INVALID_ID_MATCH_ID,
                string.Empty,
                0,
                0,
                0,
                DateTime.MinValue);
        }
    }
}