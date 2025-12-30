using GuessWhoServerDomain.Domain.Enums.Match;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct ChooseSecretCharacterResult(ChooseSecretCharacterResultCode Code)
    {
        public bool IsSuccess => Code == ChooseSecretCharacterResultCode.Sucess;

        public static ChooseSecretCharacterResult Success() => new(ChooseSecretCharacterResultCode.Sucess);

        public static ChooseSecretCharacterResult Fail(ChooseSecretCharacterResultCode code) => new(code);
    }
}
