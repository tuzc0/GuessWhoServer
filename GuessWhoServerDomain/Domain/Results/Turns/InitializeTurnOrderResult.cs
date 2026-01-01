using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct InitializeTurnOrderResult(InitializeTurnOrderResultCode Code)
    {
        public bool IsSuccess => Code == InitializeTurnOrderResultCode.Success;

        public static InitializeTurnOrderResult Success() => new(InitializeTurnOrderResultCode.Success);

        public static InitializeTurnOrderResult Fail(InitializeTurnOrderResultCode code) => new(code);
    }
}
