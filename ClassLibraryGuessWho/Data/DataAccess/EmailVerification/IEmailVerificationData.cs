using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWhoContracts.Dtos.Dto;
using System;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification
{
    public interface IEmailVerificationData
    {
        bool AddVerificationToken(CreateEmailTokenArgs args);
        EmailVerificationTokenDto GetLatestTokenByAccountId(long accountId, DateTime consumeDate);
        EmailVerificationTokenDto GetLatestTokenStatusByAccountId(long accountId);
        int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs args);
        int ConsumeToken(Guid tokenId);
        EmailVerificationResendLimitsDto GetEmailVerificationResendLimits(ResendLimitsQuery query);
        void ExpireActiveTokens(ExpireTokensArgs args);
    }
}
