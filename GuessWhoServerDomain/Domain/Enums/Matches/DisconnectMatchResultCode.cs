namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum DisconnectMatchResultCode
    {
        SuccessEndedWithWinner = 1,
        SuccessCancelledLobby = 2,
        SuccessLeftLobby = 3,
        AlreadyEndedFinished = 4,
        AlreadyEndedCancelled = 5,

        NotInMatch = 10,
        MatchNotFound = 11,
        OpponentNotFound = 12,
        OperationConflict = 13,
        InvalidArgs = 14,
        UnexpectedError = 15
    }
}
