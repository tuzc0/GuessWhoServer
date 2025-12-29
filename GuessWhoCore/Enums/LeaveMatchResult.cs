namespace GuessWhoCore.Dtos
{
    public enum LeaveMatchResult
    {
        Success = 0,
        MatchNotFound = 1,
        PlayerNotInMatch = 2,
        PlayerAlreadyLeft = 3,
        TechnicalError = 99
    }
}
