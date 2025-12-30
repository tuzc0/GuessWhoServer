namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum ChooseSecretCharacterResultCode
    {
        Sucess = 0,
        MatchNotFound = 1,
        MatchNotInProgress = 2,
        PlayerNotInMatch = 3,
        PlayerAlreadyLeft = 4,
        SecretAlreadyChosen = 5,
        InvalidCharacter = 6,
        TechnicalError = 99
    }
}
