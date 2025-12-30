using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct LeaveMatchResult(LeaveMatchResultCode Code)
    {
        public bool IsSuccess => Code == LeaveMatchResultCode.Success;

        public static LeaveMatchResult Success() => new(LeaveMatchResultCode.Success);

        public static LeaveMatchResult Fail(LeaveMatchResultCode code) => new(code); 
    }
}
