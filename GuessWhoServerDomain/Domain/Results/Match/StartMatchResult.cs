using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct StartMatchResult(StartMatchResultCode Code)
    {
        public bool IsSuccess => Code == StartMatchResultCode.Success;

        public static StartMatchResult Success() => new(StartMatchResultCode.Success);

        public static StartMatchResult Fail(StartMatchResultCode code) => new(code); 
    }
}
