namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum EndMatchResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotFound = 2,
        MatchNotInProgress = 3,
        WinnerNotInMatch = 4,
        WinnerNotResolvable = 5,
        AlreadyFinalized = 6,
        ConcurrentUpdate = 7
    }
}
