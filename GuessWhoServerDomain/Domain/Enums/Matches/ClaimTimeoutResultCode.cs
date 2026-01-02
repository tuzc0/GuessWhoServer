namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum ClaimTimeoutResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotFound = 2,
        MatchNotInProgress = 3,
        TurnStateNotInitialized = 4,
        CannotClaimOnYourTurn = 5,
        ClockStateNotAvailable = 6,
        NotTimedOut = 7,
        OperationConflict = 99
    }
}
