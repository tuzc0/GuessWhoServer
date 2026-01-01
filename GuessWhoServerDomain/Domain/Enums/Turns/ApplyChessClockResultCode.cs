namespace GuessWhoServerDomain.Domain.Enums.Turns
{
    public enum ApplyChessClockResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotInProgress = 2,
        NotYourTurn = 3, 
        PlayerNotActive = 4,
        MatchNotFound = 5,
        OperationConflict = 6
    }
}
