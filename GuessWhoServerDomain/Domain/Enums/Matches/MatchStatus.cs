namespace GuessWhoServerDomain.Domain.Enums.Matches
{
    public enum MatchStatus : byte
    {
        Lobby = 1,
        Active = 2,
        Finished = 3, 
        Cancelled = 4,
    }
}
