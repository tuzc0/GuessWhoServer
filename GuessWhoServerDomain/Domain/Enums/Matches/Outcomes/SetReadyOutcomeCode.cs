namespace GuessWhoServerDomain.Domain.Enums.Matches.Outcomes
{
    public enum SetReadyOutcomeCode
    {
        Success = 0,
        InvalidRequest = 1,
        InvalidInputs = 2,
        PlayerNotInMatch = 3,
        PlayerAlreadyLeft = 4,
        MatchNotInLobby = 5,
        OperationConflict = 6
    }
}
