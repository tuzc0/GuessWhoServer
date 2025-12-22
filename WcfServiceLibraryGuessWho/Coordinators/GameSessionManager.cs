using System;
using GuessWhoServices.Repositories.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class GameSessionManager : IGameSessionManager
    {
        private readonly IGameSessionRepository _sessionRepository;

        public GameSessionManager(IGameSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository ??
                throw new ArgumentNullException(nameof(sessionRepository));
        }

        public bool TerminateActiveSessions(long userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            return _sessionRepository.ForceLeaveActiveSessionsForUser(userId);
        }
    }
}