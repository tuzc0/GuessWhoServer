using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct SetTurnPhaseResult(SetTurnPhaseResultCode Code)
    {
        public bool IsSuccess => Code == SetTurnPhaseResultCode.Success;
        public static SetTurnPhaseResult Success() => new SetTurnPhaseResult(SetTurnPhaseResultCode.Success);
        public static SetTurnPhaseResult Fail(SetTurnPhaseResultCode code) => new SetTurnPhaseResult(code);
    }
}
