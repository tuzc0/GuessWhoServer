namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum StartMatchResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotInLobby = 2,
        NotEnoughPlayers = 3,
        PlayersNotReady = 4,
        ConcurrentUpdate = 5,
        InvalidArgs = 6,
        UnexpectedError = 7
    }
}
