namespace WcfServiceLibraryGuessWho.Services.MatchApplication
{
    public interface IMatchDeckProvider
    {
        string[] CreateDeck(long matchId, int requestedDeckSize);

        string[] GetMatchDeck(long matchId, int requestedDeckSize);
    }
}
