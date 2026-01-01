namespace GuessWhoServerDomain.Domain.Enums.Turns
{
    public enum AdvanceTurnResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotInProgress = 2,
        NotYourTurn = 3, 
        TurnStateNotInitialized = 4,
        OperationConflict = 5
    }
}
