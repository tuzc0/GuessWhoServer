using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct MarkReadyResult(MarkReadyResultCode Code)
    {
        public bool IsSuccess => Code == MarkReadyResultCode.Success;

        public static MarkReadyResult Success() => new(MarkReadyResultCode.Success);

        public static MarkReadyResult Fail(MarkReadyResultCode code) => new(code);
    }
}
