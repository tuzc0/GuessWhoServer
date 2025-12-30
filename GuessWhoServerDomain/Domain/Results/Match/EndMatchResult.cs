using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct EndMatchResult(EndMatchResultCode Code)
    {
        public bool IsSuccess => Code == EndMatchResultCode.Success;

        public static EndMatchResult Success() => new(EndMatchResultCode.Success);

        public static EndMatchResult Fail(EndMatchResultCode code) => new(code);
    }
}
