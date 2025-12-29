using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using System;

namespace ClassLibraryGuessWho.Data.Factories
{
    public sealed class GuessWhoUnitOfWorkFactory : IGuessWhoUnitOfWorkFactory
    {
        private readonly IGuessWhoDbContextFactory contextFactory;

        public GuessWhoUnitOfWorkFactory(IGuessWhoDbContextFactory contextFactory)
        {
            this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public IGuessWhoUnitOfWork Create()
        {
            GuessWhoDBEntities context = contextFactory.Create();

            IUserAccountData userAccounts = new UserAccountData(context);
            IEmailVerificationData emailVerification = new EmailVerificationData(context);

            return new GuessWhoUnitOfWork(context, userAccounts, emailVerification);
        }
    }
}
