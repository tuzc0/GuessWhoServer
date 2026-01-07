using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;

namespace GuessWhoDataAccess.Data.Factories
{
    public interface IGuessWhoUnitOfWork : IDisposable
    {
        GuessWhoDBEntities Context { get; }

        IUserAccountRepository UserAccounts { get; }
        IEmailVerificationRepository EmailVerification { get; }
        IAvatarRepository Avatars { get; }
        ICharacterRepository Characters { get; }
        IMatchDeckRepository MatchDecks { get; }
        IFriendshipRepository Friendships { get; }
        IMatchRepository Matches { get; }
        IMatchChatRepository MatchesChats { get; }
        IMatchTurnRepository MatchesTurns { get; }
        IMatchChessClockRepository MatchChessClocks { get; }
        IMatchTurnAdvanceRepository MatchTurnAdvances { get; }
        IMatchGuessingRepository MatchGuessing { get; }
        ILeaderboardRepository Leaderboards { get; }

        IGuessWhoDbTransaction BeginTransaction();
        void Flush();
    }

    public interface IGuessWhoDbTransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }

    public interface IGuessWhoUnitOfWorkFactory
    {
        IGuessWhoUnitOfWork Create();
    }
}
