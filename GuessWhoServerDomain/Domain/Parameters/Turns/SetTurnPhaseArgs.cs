using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class SetTurnPhaseArgs
    {
        public long MatchId { get; init; }
        public TurnPhase ExpectedPhase { get; init; }
        public TurnPhase NewPhase { get; init; }
    }
}
