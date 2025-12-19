using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;

namespace GuessWhoServices.Repositories.Implementation
{
    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly EmailVerificationData emailVerificationData;

        public EmailVerificationRepository(EmailVerificationData emailVerificationData)
        {
            this.emailVerificationData = emailVerificationData ?? 
                throw new ArgumentNullException(nameof(emailVerificationData));
        }

        public EmailVerificationRepository(GuessWhoDBEntities dataContext) : 
            this (new EmailVerificationData(dataContext))
        { 
        }

        public bool AddVerificationToken(CreateEmailTokenArgs args)
        {
            return emailVerificationData.AddVerificationToken(args);
        }

        public EmailVerificationTokenDto GetLatestTokenByAccountId(long accountId, DateTime consumeDate)
        {
            return emailVerificationData.GetLatestTokenByAccountId(accountId, consumeDate);
        }

        public int ConsumeToken(Guid tokenId)
        {
            return emailVerificationData.ConsumeToken(tokenId);
        }

        public int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs args)
        {
            return emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(args);
        }

        public EmailVerificationResendLimitsDto GetEmailVerificationResendLimits(ResendLimitsQuery query)
        {
            return emailVerificationData.GetEmailVerificationResendLimits(query);
        }

        public void ExpireActiveTokens(ExpireTokensArgs args)
        {
            emailVerificationData.ExpireActiveTokens(args);
        }
    }
}
