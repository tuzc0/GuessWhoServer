using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.Factories;
using GuessWhoServices.Repositories.Interfaces;
using System;

namespace GuessWhoServices.Repositories.Implementation
{
    public class GameSessionRepository : IGameSessionRepository
    {
        private readonly IGuessWhoDbContextFactory contextFactory;

        public GameSessionRepository(IGuessWhoDbContextFactory contextFactory)
        {
            this.contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        private T Execute<T>(Func<IMatchData, T> action)
        {
            using (var context = contextFactory.Create())
            {
                var matchData = new MatchData(context);
                return action(matchData);
            }
        }

        public bool ForceLeaveActiveSessionsForUser(long userId)
        {
            return Execute(data => data.ForceLeaveAllMatchesForUser(userId));
        }
    }
}