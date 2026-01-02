using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct ChangeSecretCharacterResult(ChangeSecretCharacterResultCode Code)
    {
        public bool IsSuccess => Code == ChangeSecretCharacterResultCode.Success;

        public static ChangeSecretCharacterResult Success() => new(ChangeSecretCharacterResultCode.Success);

        public static ChangeSecretCharacterResult Fail(ChangeSecretCharacterResultCode code) => new(code);
    }
}
