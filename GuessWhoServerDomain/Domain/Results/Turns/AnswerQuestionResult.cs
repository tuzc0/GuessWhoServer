using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct AnswerQuestionResult(AnswerQuestionResultCode Code)
    {
        public bool IsSuccess => Code == AnswerQuestionResultCode.Success;

        public static AnswerQuestionResult Success() => new AnswerQuestionResult(AnswerQuestionResultCode.Success);
        public static AnswerQuestionResult Fail(AnswerQuestionResultCode code) => new AnswerQuestionResult(code);
    }
}
