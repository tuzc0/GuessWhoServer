using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using ClassLibraryGuessWho.Data.Factories;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;

namespace GuessWhoServices.Repositories.Implementation
{
    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly IGuessWhoDbContextFactory contextFactory;

        public EmailVerificationRepository(IGuessWhoDbContextFactory contextFactory)
        {
            this.contextFactory = contextFactory ?? 
                throw new ArgumentNullException(nameof(contextFactory));
        }

        private T Execute<T>(Func<IEmailVerificationData, T> action)
        {
            using (var context = contextFactory.Create())
            {
                var emailVerificationData = new EmailVerificationData(context);
                return action(emailVerificationData);
            }
        }

        private void Execute(Action<IEmailVerificationData> action)
        {
            using (var context = contextFactory.Create())
            {
                var emailVerificationData = new EmailVerificationData(context);
                action(emailVerificationData);
            }
        }

        public bool AddVerificationToken(CreateEmailTokenArgs args) =>
            Execute(emailVerificationData => emailVerificationData.AddVerificationToken(args));

        public EmailVerificationTokenDto GetLatestTokenByAccountId(long accountId, DateTime consumeDate) =>
            Execute(emailVerificationData => emailVerificationData.GetLatestTokenByAccountId(accountId, consumeDate));

        public EmailVerificationTokenDto GetLatestTokenStatusByAccountId(long accountId) => 
            Execute(emailVerificationData => emailVerificationData.GetLatestTokenStatusByAccountId(accountId));

        public int ConsumeToken(Guid tokenId) => 
            Execute(emailVerificationData => emailVerificationData.ConsumeToken(tokenId));

        public int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs args) => 
            Execute(emailVerificationData => emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(args));

        public EmailVerificationResendLimitsDto GetEmailVerificationResendLimits(ResendLimitsQuery query) => 
            Execute(emailVerificationData => emailVerificationData.GetEmailVerificationResendLimits(query));

        public void ExpireActiveTokens(ExpireTokensArgs args) =>
            Execute(emailVerificationData => emailVerificationData.ExpireActiveTokens(args));
    }
}
