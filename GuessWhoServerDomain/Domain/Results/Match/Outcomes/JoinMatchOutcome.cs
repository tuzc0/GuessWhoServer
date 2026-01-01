using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Models.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match.Outcomes
{
    public readonly record struct JoinMatchOutcome(JoinMatchOutcomeCode Code, JoinMatchSnapshot Snapshot)
    {
        public bool IsSuccess => Code == JoinMatchOutcomeCode.Success;

        public static JoinMatchOutcome Success(JoinMatchSnapshot snapshot)
        {
            return new JoinMatchOutcome(
                JoinMatchOutcomeCode.Success,
                snapshot ?? JoinMatchSnapshot.Empty());
        }

        public static JoinMatchOutcome Fail(JoinMatchOutcomeCode code)
        {
            return new JoinMatchOutcome(code, JoinMatchSnapshot.Empty());
        }
    }
}
