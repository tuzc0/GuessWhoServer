namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum MatchStatus : byte
    {
        Lobby = 1,
        Active = 2,
        Finished = 3, 
        Cancelled = 4,
    }
}
