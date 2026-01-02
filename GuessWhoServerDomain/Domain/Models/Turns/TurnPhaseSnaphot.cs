using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Models.Turns
{
    public readonly record struct TurnPhaseSnapshot(
        long MatchId,
        TurnPhase Phase)
    {
        private const long INVALID_ID = 0;

        public bool IsValid => MatchId > INVALID_ID;
        public static TurnPhaseSnapshot Invalid() => new TurnPhaseSnapshot(0, TurnPhase.Main);
    }
}
