using System;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface IGameSessionManager
    {
        bool TerminateActiveSessions(long userId);
    }
}