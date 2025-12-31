namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum ChangeSecretCharacterResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotInLobby = 2,
        PlayerNotInMatch = 3,
        PlayerAlreadyLeft = 4,
        InvalidCharacter = 5,
        OperationConflict = 6
    }
}
