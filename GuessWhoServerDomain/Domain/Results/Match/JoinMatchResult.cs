using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct JoinMatchResult(

        JoinMatchResultCode Code,
        long MatchId)
    {
        public bool IsValid => Code == JoinMatchResultCode.Sucess;

        public static JoinMatchResult Success(long matchId) => new(JoinMatchResultCode.Sucess, matchId);

        public static JoinMatchResult Fail(JoinMatchResultCode code, long matchId) => new(code, matchId);
    }
}
