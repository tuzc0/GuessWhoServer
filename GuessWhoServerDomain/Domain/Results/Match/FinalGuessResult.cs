using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct FinalGuessResult(FinalGuessResultCode Code, bool IsCorrect)
    {
        public bool IsSuccess => Code == FinalGuessResultCode.Success;

        public static FinalGuessResult Success(bool isCorrect) =>
            new(FinalGuessResultCode.Success, isCorrect);

        public static FinalGuessResult Fail(FinalGuessResultCode code) =>
            new(code, false);
    }
}
