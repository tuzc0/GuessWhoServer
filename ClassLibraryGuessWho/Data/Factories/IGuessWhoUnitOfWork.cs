using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;

namespace ClassLibraryGuessWho.Data.Factories
{
    public interface IGuessWhoUnitOfWork : IDisposable
    {
        GuessWhoDBEntities Context { get; }

        IUserAccountRepository UserAccounts { get; }
        IEmailVerificationRepository EmailVerification { get; }

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
