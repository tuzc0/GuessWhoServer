using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct SetMatchPrivateResult(SetMatchPrivateResultCode Code)
    {
        public bool IsSuccess => Code == SetMatchPrivateResultCode.Success;

        public static SetMatchPrivateResult Success() => new(SetMatchPrivateResultCode.Success);

        public static SetMatchPrivateResult Fail(SetMatchPrivateResultCode code) => new(code);
    }
}
