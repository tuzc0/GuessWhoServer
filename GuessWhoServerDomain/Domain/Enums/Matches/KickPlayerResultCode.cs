namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum KickPlayerResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotInLobby = 2,
        HostNotInMatch = 3,
        HostNotAuthorized = 4,
        TargetNotInMatch = 5,
        TargetAlreadyLeft = 6,
        CannotKickHost = 7,
        ConcurrentError = 99
    }
}
