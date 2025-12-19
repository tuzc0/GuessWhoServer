namespace GuessWhoContracts.Enums
{
    public enum ChooseSecretCharacterResult
    {
        Success,
        MatchNotFound,
        MatchNotInProgress,
        PlayerNotInMatch,
        PlayerAlreadyLeft,
        SecretAlreadyChosen,
        InvalidCharacter
    }
}
