namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IGameSessionManager
    {
        bool TerminateActiveSessions(long userId);
    }
}