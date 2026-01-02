using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct EndMatchResult(EndMatchResultCode Code, long WinnerUserId)
    {
        private const long INVALID_ID = 0;

        public bool IsSuccess => Code == EndMatchResultCode.Success;

        public static EndMatchResult Success(long winnerUserId)
        {
            return new EndMatchResult(EndMatchResultCode.Success, winnerUserId > INVALID_ID ? winnerUserId : INVALID_ID);
        }

        public static EndMatchResult Fail(EndMatchResultCode code) => new(code, INVALID_ID);
    }
}
