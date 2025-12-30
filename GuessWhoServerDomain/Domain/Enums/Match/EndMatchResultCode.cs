namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum EndMatchResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotInProgress = 2,
        WinnerNotInMatch = 3,
        TechnicalError = 99
    }
}
