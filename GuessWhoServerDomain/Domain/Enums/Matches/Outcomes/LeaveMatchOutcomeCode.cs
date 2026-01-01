namespace GuessWhoServerDomain.Domain.Enums.Matches.Outcomes
{
    public enum LeaveMatchOutcomeCode
    {
        Success = 0,
        InvalidRequest = 1,
        InvalidInputs = 2,
        MatchNotFound = 3,
        PlayerNotInMatch = 4,
        PlayerAlreadyLeft = 5,
        OperationConflict = 6
    }
}
