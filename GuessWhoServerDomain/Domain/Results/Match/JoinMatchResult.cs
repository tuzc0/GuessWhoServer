using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct JoinMatchResult(

        JoinMatchResultCode Code,
        long MatchId)
    {
        public bool IsValid => Code == JoinMatchResultCode.Success;

        public static JoinMatchResult Success(long matchId) => new(JoinMatchResultCode.Success, matchId);

        public static JoinMatchResult Fail(JoinMatchResultCode code, long matchId) => new(code, matchId);
    }
}
