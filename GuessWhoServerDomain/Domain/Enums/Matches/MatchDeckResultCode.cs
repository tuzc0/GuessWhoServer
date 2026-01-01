namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum MatchDeckResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        InvalidMatchId = 2,
        DeckNotFound = 3,
        DeckAlreadyExists = 4,
        InsufficientCharacters = 5,
        OperationConflict = 6,
        DeckGenerationFailed = 7
    }
}
