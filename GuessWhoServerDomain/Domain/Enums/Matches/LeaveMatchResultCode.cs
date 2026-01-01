namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum LeaveMatchResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        PlayerNotInMatch = 2,
        PlayerAlreadyLeft = 3,
        TechnicalError = 99
    }
}
