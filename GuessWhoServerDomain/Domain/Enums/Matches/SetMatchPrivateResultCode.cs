namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum SetMatchPrivateResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotInLobby = 2,
        HostNotAuthorized = 3,
        AlreadyPrivate = 4,
        InvalidArgs = 5,
        ConcurrentUpdate = 6,
        UnexpectedError = 7
    }
}
