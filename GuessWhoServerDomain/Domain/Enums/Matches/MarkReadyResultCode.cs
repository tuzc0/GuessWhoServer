namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum MarkReadyResultCode
    {
        Success = 0,
        PlayerNotFound = 1, 
        PlayerAlreadyLeft = 2,
        MatchNotInLobby = 3,
        InvalidArgs = 4
    }
}
