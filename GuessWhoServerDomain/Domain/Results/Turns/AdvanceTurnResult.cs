using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Models.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct AdvanceTurnResult(
        AdvanceTurnResultCode Code,
        TurnStateSnapshot TurnState
    )
    {
        public bool IsSuccess => Code == AdvanceTurnResultCode.Success;

        public static AdvanceTurnResult Success(TurnStateSnapshot snapshot) =>
            new(AdvanceTurnResultCode.Success, snapshot);

        public static AdvanceTurnResult Fail(AdvanceTurnResultCode code) =>
            new(code, default);
    }
}
