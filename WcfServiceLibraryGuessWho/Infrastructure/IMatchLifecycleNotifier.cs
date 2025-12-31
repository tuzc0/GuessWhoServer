namespace GuessWhoServices.Infrastructure
{
    public interface IMatchLifecycleNotifier
    {
        void NotifyMatchStarted(long matchId);
        void NotifyMatchEnded(long matchId, long winnerUserId);
        void NotifySecretCharacterChosen(long matchId, long userId);
        void NotifyAllSecretCharactersChosen(long matchId);
    }
}
