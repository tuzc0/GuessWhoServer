namespace GuessWhoServerDomain.Domain.Enums.Turns
{
    public enum InitializeTurnOrderResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotFound = 2,
        MatchNotInProgress = 3,
        PlayersNotInMatch = 4,
        AlreadyInitialized = 5,
        OperationConflict = 6
    }
}
