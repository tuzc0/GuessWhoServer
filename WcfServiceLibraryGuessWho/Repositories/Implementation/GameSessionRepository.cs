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

        // Método genérico Execute (Estándar del equipo)
        // Nota: El Func recibe IMatchData porque es la clase que ya tienes con la lógica
        private T Execute<T>(Func<IMatchData, T> action)
        {
            using (var context = contextFactory.Create())
            {
                // Instanciamos tu clase MatchData con el contexto de la Factory
                var matchData = new MatchData(context);
                return action(matchData);
            }
        }

        // El método de la interfaz IGameSessionRepository
        public bool ForceLeaveActiveSessionsForUser(long userId)
        {
            // Llamamos al método que ya tienes programado en MatchData
            return Execute(data => data.ForceLeaveAllMatchesForUser(userId));
        }
    }
}