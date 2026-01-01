namespace GuessWhoServerDomain.Domain.Enums.Matches.Outcomes
{
    public enum JoinMatchOutcomeCode
    {
        Success = 0,
        InvalidRequest = 1,
        InvalidInputs = 2,
        MatchNotFound = 3,
        MatchNotJoinable = 4,
        MatchFull = 5,
        PlayerAlreadyInMatch = 6,
        InOtherActiveMatch = 7,
        HostNotFound = 8,
        OperationConflict = 9
    }
}
