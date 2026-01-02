using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct RegisterQuestionResult(RegisterQuestionResultCode Code)
    {
        public bool IsSuccess => Code == RegisterQuestionResultCode.Success;
        public static RegisterQuestionResult Success() => new RegisterQuestionResult(RegisterQuestionResultCode.Success);
        public static RegisterQuestionResult Fail(RegisterQuestionResultCode code) => new RegisterQuestionResult(code);
    }
}
