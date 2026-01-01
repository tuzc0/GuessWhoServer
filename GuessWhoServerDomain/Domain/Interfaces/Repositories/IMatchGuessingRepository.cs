using GuessWhoServerDomain.Domain.Results.Match;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchGuessingRepository
    {
        LoadFinalGuessSnapshotResult LoadFinalGuessSnapshot(long matchId, long guessingUserId);
    }
}
