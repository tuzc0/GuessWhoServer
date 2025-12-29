namespace GuessWhoCore.Dtos
{
    public enum KickPlayerResult
    {
        Success,
        MatchNotFound,
        MatchNotInLobby,
        RequesterNotInMatch,
        RequesterAlreadyLeft,
        RequesterNotHost,
        TargetNotInMatch,
        TargetAlreadyLeft
    }
}
