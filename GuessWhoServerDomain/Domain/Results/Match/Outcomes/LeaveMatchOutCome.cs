using GuessWhoServerDomain.Domain.Enums.Matches.Outcomes;

namespace GuessWhoServerDomain.Domain.Results.Match.Outcomes
{
    public readonly record struct LeaveMatchOutcome(LeaveMatchOutcomeCode Code)
    {
        public bool IsSuccess => Code == LeaveMatchOutcomeCode.Success;

        public static LeaveMatchOutcome Success() => new(LeaveMatchOutcomeCode.Success);

        public static LeaveMatchOutcome Fail(LeaveMatchOutcomeCode code) => new(code);
    }
}
}
