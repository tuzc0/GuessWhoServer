namespace GuessWhoServices.Repositories.Interfaces
{
    public interface IGameSessionRepository
    {
        bool ForceLeaveActiveSessionsForUser(long userId);
    }
}