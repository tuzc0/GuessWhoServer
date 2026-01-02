using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct KickPlayerResult(KickPlayerResultCode Code)
    {
        public bool IsSuccess => Code == KickPlayerResultCode.Success;

        public static KickPlayerResult Success() => new(KickPlayerResultCode.Success);

        public static KickPlayerResult Fail(KickPlayerResultCode code) => new(code);
    }
}
