using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct ChooseSecretCharacterResult(ChooseSecretCharacterResultCode Code)
    {
        public bool IsSuccess => Code == ChooseSecretCharacterResultCode.Success;

        public static ChooseSecretCharacterResult Success() => new(ChooseSecretCharacterResultCode.Success);

        public static ChooseSecretCharacterResult Fail(ChooseSecretCharacterResultCode code) => new(code);
    }
}
