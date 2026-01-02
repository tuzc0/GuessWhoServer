using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct AskQuestionResult(AskQuestionResultCode Code)
    {
        public bool IsSuccess => Code == AskQuestionResultCode.Success;

        public static AskQuestionResult Success() => new AskQuestionResult(AskQuestionResultCode.Success);
        public static AskQuestionResult Fail(AskQuestionResultCode code) => new AskQuestionResult(code);
    }
}
