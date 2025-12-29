using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using System;
using System.Data.Entity;

namespace ClassLibraryGuessWho.Data.Factories
{
    public sealed class GuessWhoUnitOfWork : IGuessWhoUnitOfWork
    {
        private readonly GuessWhoDBEntities context;

        public GuessWhoUnitOfWork(
            GuessWhoDBEntities context,
            IUserAccountData userAccounts,
            IEmailVerificationData emailVerification)
        {
            this.context = context ??
                throw new ArgumentNullException(nameof(context));
            UserAccounts = userAccounts ??
                throw new ArgumentNullException(nameof(userAccounts));
            EmailVerification = emailVerification ??
                throw new ArgumentNullException(nameof(emailVerification));
        }

        public GuessWhoDBEntities Context => context;

        public IUserAccountData UserAccounts { get; }

        public IEmailVerificationData EmailVerification { get; }

        public IGuessWhoDbTransaction BeginTransaction()
        {
            DbContextTransaction efTransaction = context.Database.BeginTransaction();
            return new EfDbTransaction(efTransaction);
        }

        public void Flush()
        {
            context.SaveChanges();
        }

        public void Dispose()
        {
            context.Dispose();
        }

        private sealed class EfDbTransaction : IGuessWhoDbTransaction
        {
            private readonly DbContextTransaction transaction;

            public EfDbTransaction(DbContextTransaction transaction)
            {
                this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            }

            public void Commit()
            {
                transaction.Commit();
            }

            public void Rollback()
            {
                transaction.Rollback();
            }

            public void Dispose()
            {
                transaction.Dispose();
            }
        }
    }
}
