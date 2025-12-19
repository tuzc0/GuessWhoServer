using System;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWhoContracts.Dtos.Dto;

namespace GuessWhoServices.Repositories.Interfaces
{
    public interface IEmailVerificationRepository
    {
        bool AddVerificationToken(CreateEmailTokenArgs args);

        EmailVerificationTokenDto GetLatestTokenByAccountId(long accountId, DateTime consumeDate);

        int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs args);

        int ConsumeToken(Guid tokenId);

        EmailVerificationResendLimitsDto GetEmailVerificationResendLimits(ResendLimitsQuery query);

        void ExpireActiveTokens(ExpireTokensArgs args);
    }
}
