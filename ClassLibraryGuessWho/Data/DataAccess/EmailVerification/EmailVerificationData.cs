using ClassLibraryGuessWho.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Results;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification
{
    public sealed class EmailVerificationData : IEmailVerificationRepository
    {
        private const int HOURS_BACK_FOR_LIMIT = 1;

        private const string SQL_INCREMENT_FAILED_ATTEMPTS_AND_MAYBE_EXPIRE =
            @"UPDATE dbo.EMAIL_VERIFICATION
              SET FAILEDATTEMPTS = FAILEDATTEMPTS + 1,
                  EXPIRESUTC = CASE WHEN FAILEDATTEMPTS + 1 >= @p0 THEN @p1 ELSE EXPIRESUTC END
              WHERE TOKENID = @p2 AND CONSUMEDUTC IS NULL AND EXPIRESUTC >= @p1";

        private const string SQL_CONSUME_TOKEN =
            @"UPDATE dbo.EMAIL_VERIFICATION
              SET CONSUMEDUTC = SYSUTCDATETIME()
              WHERE TOKENID = @p0 AND CONSUMEDUTC IS NULL";

        private const string SQL_EXPIRE_ACTIVE_TOKENS =
            @"UPDATE dbo.EMAIL_VERIFICATION
              SET EXPIRESUTC = @p0
              WHERE ACCOUNTID = @p1
                AND CONSUMEDUTC IS NULL
                AND EXPIRESUTC > @p0";

        private readonly GuessWhoDBEntities dataContext;

        public EmailVerificationData(GuessWhoDBEntities context)
        {
            dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public bool AddVerificationToken(CreateEmailTokenArgs emailTokenArgs)
        {
            if (emailTokenArgs == null)
            {
                throw new ArgumentNullException(nameof(emailTokenArgs));
            }

            var tokenEntity = new EMAIL_VERIFICATION
            {
                TOKENID = Guid.NewGuid(),
                ACCOUNTID = emailTokenArgs.AccountId,
                CODEHASH = emailTokenArgs.CodeHash ?? Array.Empty<byte>(),
                CREATEDATUTC = emailTokenArgs.NowUtc,
                EXPIRESUTC = emailTokenArgs.NowUtc.Add(emailTokenArgs.LifeSpan),
                CONSUMEDUTC = null
            };

            dataContext.EMAIL_VERIFICATION.Add(tokenEntity);

            return true;
        }

        public EmailVerificationTokenRecord GetLatestActiveTokenByAccountId(long accountId, DateTime nowUtc)
        {
            EMAIL_VERIFICATION tokenEntity = GetLatestTokenEntity(accountId, 
                t => t.EXPIRESUTC >= nowUtc && t.CONSUMEDUTC == null);

            return EmailVerificationTokenRecordMapper.ToRecord(tokenEntity);
        }

        public EmailVerificationTokenRecord GetLatestTokenStatusByAccountId(long accountId)
        {
            EMAIL_VERIFICATION tokenEntity = GetLatestTokenEntity(accountId, predicate: null);

            return EmailVerificationTokenRecordMapper.ToRecord(tokenEntity);
        }

        public int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs failedAttemptArgs)
        {
            if (failedAttemptArgs == null)
            {
                throw new ArgumentNullException(nameof(failedAttemptArgs));
            }

            if (failedAttemptArgs.TokenId == Guid.Empty)
            {
                throw new ArgumentException("TokenId cannot be empty.", nameof(failedAttemptArgs));
            }

            return dataContext.Database.ExecuteSqlCommand(
                SQL_INCREMENT_FAILED_ATTEMPTS_AND_MAYBE_EXPIRE,
                failedAttemptArgs.MaxAttempts,
                failedAttemptArgs.NowUtc,
                failedAttemptArgs.TokenId);
        }

        public int ConsumeToken(Guid tokenId)
        {
            if (tokenId == Guid.Empty)
            {
                throw new ArgumentException("tokenId cannot be empty.", nameof(tokenId));
            }

            return dataContext.Database.ExecuteSqlCommand(
                SQL_CONSUME_TOKEN,
                tokenId);
        }

        public EmailVerificationResendLimitsResult GetEmailVerificationResendLimits(ResendLimitsQuery limitsQuery)
        {
            if (limitsQuery == null)
            {
                throw new ArgumentNullException(nameof(limitsQuery));
            }

            DateTime? lastTokenCreatedAtUtc = GetLastTokenCreatedAtUtc(limitsQuery.AccountId);

            DateTime windowStartUtc = limitsQuery.DateUtc.AddHours(-HOURS_BACK_FOR_LIMIT);
            int tokensSentInWindow = CountTokensCreatedSince(limitsQuery.AccountId, windowStartUtc);

            bool isPerMinuteCooldownActive = IsCooldownActive(
                lastTokenCreatedAtUtc,
                limitsQuery.DateUtc,
                limitsQuery.CooldownSeconds);

            bool isWithinHourlyLimit = tokensSentInWindow < limitsQuery.HourlyMaxTokens;

            return new EmailVerificationResendLimitsResult
            {
                IsPerMinuteCooldownActive = isPerMinuteCooldownActive,
                IsWithinHourlyLimit = isWithinHourlyLimit,
                LastTokenCreatedAtUtc = lastTokenCreatedAtUtc,
                TokensSentInLastHour = tokensSentInWindow
            };
        }

        public void ExpireActiveTokens(ExpireTokensArgs expireTokensArgs)
        {
            if (expireTokensArgs == null)
            {
                throw new ArgumentNullException(nameof(expireTokensArgs));
            }

            dataContext.Database.ExecuteSqlCommand(
                SQL_EXPIRE_ACTIVE_TOKENS,
                expireTokensArgs.NewExpirationUtc,
                expireTokensArgs.AccountId);
        }

        private EMAIL_VERIFICATION GetLatestTokenEntity(long accountId, Expression<Func<EMAIL_VERIFICATION, bool>> predicate)
        {
            IQueryable<EMAIL_VERIFICATION> query = dataContext.EMAIL_VERIFICATION
                .AsNoTracking()
                .Where(t => t.ACCOUNTID == accountId);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query
                .OrderByDescending(t => t.CREATEDATUTC)
                .FirstOrDefault();
        }

        private DateTime? GetLastTokenCreatedAtUtc(long accountId)
        {
            return dataContext.EMAIL_VERIFICATION
                .AsNoTracking()
                .Where(t => t.ACCOUNTID == accountId)
                .OrderByDescending(t => t.CREATEDATUTC)
                .Select(t => (DateTime?)t.CREATEDATUTC)
                .FirstOrDefault();
        }

        private int CountTokensCreatedSince(long accountId, DateTime sinceUtc)
        {
            return dataContext.EMAIL_VERIFICATION
                .AsNoTracking()
                .Count(t => t.ACCOUNTID == accountId && t.CREATEDATUTC >= sinceUtc);
        }

        private static bool IsCooldownActive(DateTime? lastTokenCreatedAtUtc, DateTime nowUtc, int cooldownSeconds)
        {
            if (!lastTokenCreatedAtUtc.HasValue)
            {
                return false;
            }

            return (nowUtc - lastTokenCreatedAtUtc.Value).TotalSeconds < cooldownSeconds;
        }
    }
}
