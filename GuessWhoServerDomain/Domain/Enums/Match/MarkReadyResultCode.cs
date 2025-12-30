namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum MarkReadyResultCode
    {
        Success = 0,
        PlayerNotFound = 1, 
        PlayerAlreadyLeft = 2,
        MatchNotInLobby = 3,
        TechnicalError = 4
    }
}
