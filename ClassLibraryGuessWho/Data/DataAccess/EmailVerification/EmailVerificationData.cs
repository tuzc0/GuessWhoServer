using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWhoContracts.Dtos.Dto;
using System;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification
{
    public sealed class EmailVerificationData : IEmailVerificationData
    {
        private const int MIN_AFFECTED_ROWS = 1;
        private const int RESEND_COOLDOWN_SECONDS = 60;
        private const int RESEND_HOURLY_MAX_TOKENS = 5;

        private readonly GuessWhoDBEntities dataBaseContext;

        public EmailVerificationData(GuessWhoDBEntities context)
        {
            dataBaseContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public bool AddVerificationToken(CreateEmailTokenArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var tokenEntity = new EMAIL_VERIFICATION
            {
                TOKENID = Guid.NewGuid(),
                ACCOUNTID = args.AccountId,
                CODEHASH = args.CodeHash,
                CREATEDATUTC = args.NowUtc,
                EXPIRESUTC = args.NowUtc.Add(args.LifeSpan),
                CONSUMEDUTC = null
            };

            dataBaseContext.EMAIL_VERIFICATION.Add(tokenEntity);

            int affectedRows = dataBaseContext.SaveChanges();
            bool isCreated = affectedRows >= MIN_AFFECTED_ROWS;

            return isCreated;
        }

        public EmailVerificationTokenDto GetLatestTokenByAccountId(long accountId, DateTime consumeDate)
        {
            EmailVerificationTokenDto emailVerificationToken;

            var tokenEntity = dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == accountId &&
                                t.EXPIRESUTC >= consumeDate &&
                                t.CONSUMEDUTC == null)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .FirstOrDefault();

            if (tokenEntity == null)
            {
                emailVerificationToken = EmailVerificationTokenDto.CreateInvalid();
            }
            else
            {
                emailVerificationToken = new EmailVerificationTokenDto
                {
                    TokenId = tokenEntity.TOKENID,
                    AccountId = tokenEntity.ACCOUNTID,
                    CodeHash = tokenEntity.CODEHASH,
                    CreatedAtUtc = tokenEntity.CREATEDATUTC,
                    ExpiresUtc = tokenEntity.EXPIRESUTC,
                    ConsumedUtc = tokenEntity.CONSUMEDUTC
                };
            }

            return emailVerificationToken;
        }

        public EmailVerificationTokenDto GetLatestTokenStatusByAccountId(long accountId)
        {
            EmailVerificationTokenDto emailVerificationToken;

            var tokenEntity = dataBaseContext.EMAIL_VERIFICATION
                .Where(t => t.ACCOUNTID == accountId)
                .OrderByDescending(t => t.CREATEDATUTC)
                .FirstOrDefault();

            if (tokenEntity == null)
            {
                emailVerificationToken = EmailVerificationTokenDto.CreateInvalid();
            }
            else
            {
                emailVerificationToken = new EmailVerificationTokenDto
                {
                    TokenId = tokenEntity.TOKENID,
                    AccountId = tokenEntity.ACCOUNTID,
                    CodeHash = tokenEntity.CODEHASH,
                    CreatedAtUtc = tokenEntity.CREATEDATUTC,
                    ExpiresUtc = tokenEntity.EXPIRESUTC,
                    ConsumedUtc = tokenEntity.CONSUMEDUTC
                };
            }

            return emailVerificationToken;
        }


        public int IncrementFailedAttemptsAndMaybeExpire(IncrementFailedAttemptArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return dataBaseContext.Database.ExecuteSqlCommand(
                   @"UPDATE dbo.EMAIL_VERIFICATION
                      SET FAILEDATTEMPTS = FAILEDATTEMPTS + 1,
                          EXPIRESUTC = CASE WHEN FAILEDATTEMPTS + 1 >= @p0 THEN @p1 ELSE EXPIRESUTC END
                      WHERE TOKENID = @p2 AND CONSUMEDUTC IS NULL AND EXPIRESUTC >= @p1",
                   args.MaxAttempts, args.NowUtc, args.TokenId);
        }

        public int ConsumeToken(Guid tokenId)
        {
            return dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                      SET CONSUMEDUTC = SYSUTCDATETIME()
                      WHERE TOKENID = @p0 AND CONSUMEDUTC IS NULL",
                    tokenId);
        }

        public EmailVerificationResendLimitsDto GetEmailVerificationResendLimits(ResendLimitsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            DateTime? lastTokenCreatedAtUtc = dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == query.AccountId)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .Select(t => (DateTime?)t.CREATEDATUTC)
                    .FirstOrDefault();

            DateTime oneHourAgoUtc = query.DateUtc.AddHours(-1);

            int tokensSentInLastHour = dataBaseContext.EMAIL_VERIFICATION
                .Count(t => t.ACCOUNTID == query.AccountId && t.CREATEDATUTC >= oneHourAgoUtc);

            bool isPerMinuteCooldownActive =
                lastTokenCreatedAtUtc.HasValue &&
                (query.DateUtc - lastTokenCreatedAtUtc.Value).TotalSeconds < RESEND_COOLDOWN_SECONDS;

            bool isWithinHourlyLimit = tokensSentInLastHour < RESEND_HOURLY_MAX_TOKENS;

            return new EmailVerificationResendLimitsDto
            {
                IsPerMinuteCooldownActive = isPerMinuteCooldownActive,
                IsWithinHourlyLimit = isWithinHourlyLimit,
                LastTokenCreatedAtUtc = lastTokenCreatedAtUtc,
                TokensSentInLastHour = tokensSentInLastHour
            };
        }

        public void ExpireActiveTokens(ExpireTokensArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                      SET EXPIRESUTC = @p0
                      WHERE ACCOUNTID = @p1
                        AND CONSUMEDUTC IS NULL
                        AND EXPIRESUTC > @p0",
                    args.NewExpirationUtc, args.AccountId);
        }
    }
}
