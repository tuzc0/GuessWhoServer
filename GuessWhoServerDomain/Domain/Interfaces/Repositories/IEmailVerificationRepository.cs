using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Results;
using System;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IEmailVerificationRepository
    {
        bool AddVerificationToken(CreateEmailTokenArgs emailTokenArgs);

        EmailVerificationTokenRecord GetLatestActiveTokenByAccountId(long accountId, DateTime nowUtc);

        EmailVerificationTokenRecord GetLatestTokenStatusByAccountId(long accountId);

        int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs failedAttemptArgs);

        int ConsumeToken(Guid tokenId);

        EmailVerificationResendLimitsResult GetEmailVerificationResendLimits(ResendLimitsQuery limitsQuery);

        void ExpireActiveTokens(ExpireTokensArgs expireTokensArgs);
    }
}
