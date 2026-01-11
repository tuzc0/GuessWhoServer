using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct SetMatchVisibilityResult(SetMatchVisibilityResultCode Code)
    {
        public bool IsSuccess => Code == SetMatchVisibilityResultCode.Success;

        public static SetMatchVisibilityResult Success() => new(SetMatchVisibilityResultCode.Success);
        public static SetMatchVisibilityResult Fail(SetMatchVisibilityResultCode code) => new(code);
    }
}
