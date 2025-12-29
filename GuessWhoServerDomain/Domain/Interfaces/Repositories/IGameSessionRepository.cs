namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IGameSessionRepository
    {
        bool ForceLeaveActiveSessionsForUser(long userId);
    }
}