using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct RegisterAnswerResult(RegisterAnswerResultCode Code)
    {
        public bool IsSuccess => Code == RegisterAnswerResultCode.Success;
        public static RegisterAnswerResult Success() => new RegisterAnswerResult(RegisterAnswerResultCode.Success);
        public static RegisterAnswerResult Fail(RegisterAnswerResultCode code) => new RegisterAnswerResult(code);
    }
}
