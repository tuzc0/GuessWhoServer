using GuessWhoServerDomain.Domain.Enums.Matches.Outcomes;

namespace GuessWhoServerDomain.Domain.Results.Match.Outcomes
{
    public readonly record struct SetReadyOutcome(SetReadyOutcomeCode Code)
    {
        public bool IsSuccess => Code == SetReadyOutcomeCode.Success;

        public static SetReadyOutcome Success() => new(SetReadyOutcomeCode.Success);

        public static SetReadyOutcome Fail(SetReadyOutcomeCode code) => new(code);
    }
}
