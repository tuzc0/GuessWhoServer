using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWhoContracts.Dtos.Dto;
using System;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification
{
    public sealed class EmailVerificationData
    {
        private const int MIN_AFFECTED_ROWS = 1;
        private const int RESEND_COOLDOWN_SECONDS = 60;
        private const int RESEND_HOURLY_MAX_TOKENS = 5;

        public bool AddVerificationToken(CreateEmailTokenArgs args)
        { 
            bool isCreated = false;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var tokenEntity = new EMAIL_VERIFICATION
                {
                    ACCOUNTID = args.AccountId,
                    CODEHASH = args.CodeHash,
                    CREATEDATUTC = args.NowUtc,
                    EXPIRESUTC = args.NowUtc.Add(args.LifeSpan),
                    CONSUMEDUTC = null
                };

                dataBaseContext.EMAIL_VERIFICATION.Add(tokenEntity);

                int affectedRows = dataBaseContext.SaveChanges();
                isCreated = affectedRows >= MIN_AFFECTED_ROWS;
            }

            return isCreated;
        }

        public EmailVerificationTokenDto GetLatestTokenByAccountId(long accountId, DateTime consumeDate)
        {
            EmailVerificationTokenDto emailVerificationToken;

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
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
        }

        public int IncrementFailedAttemptsAndMaybeExpire(Guid tokenId, DateTime nowUtc, int maxAttempts = 5)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                      SET FAILEDATTEMPTS = FAILEDATTEMPTS + 1,
                          EXPIRESUTC = CASE WHEN FAILEDATTEMPTS + 1 >= @p0 THEN @p1 ELSE EXPIRESUTC END
                      WHERE TOKENID = @p2 AND CONSUMEDUTC IS NULL AND EXPIRESUTC >= @p1",
                    maxAttempts, nowUtc, tokenId);
            }
        }

        public int ConsumeToken(Guid tokenId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                      SET CONSUMEDUTC = SYSUTCDATETIME()
                      WHERE TOKENID = @p0 AND CONSUMEDUTC IS NULL",
                    tokenId);
            }
        }

        public EmailVerificationResendLimitsDto GetEmailVerificationResendLimits(long accountId, DateTime dateUtc)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                DateTime? lastTokenCreatedAtUtc = dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == accountId)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .Select(t => (DateTime?)t.CREATEDATUTC)
                    .FirstOrDefault();

                DateTime oneHourAgoUtc = dateUtc.AddHours(-1);

                int tokensSentInLastHour = dataBaseContext.EMAIL_VERIFICATION
                    .Count(t => t.ACCOUNTID == accountId && t.CREATEDATUTC >= oneHourAgoUtc);

                bool isPerMinuteCooldownActive =
                    lastTokenCreatedAtUtc.HasValue &&
                    (dateUtc - lastTokenCreatedAtUtc.Value).TotalSeconds < RESEND_COOLDOWN_SECONDS;

                bool isWithinHourlyLimit = tokensSentInLastHour < RESEND_HOURLY_MAX_TOKENS;

                return new EmailVerificationResendLimitsDto
                {
                    IsPerMinuteCooldownActive = isPerMinuteCooldownActive,
                    IsWithinHourlyLimit = isWithinHourlyLimit,
                    LastTokenCreatedAtUtc = lastTokenCreatedAtUtc,
                    TokensSentInLastHour = tokensSentInLastHour
                };
            }
        }

        public void ExpireActiveTokens(long accountId, DateTime date)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                dataBaseContext.Database.ExecuteSqlCommand(
                    @"UPDATE dbo.EMAIL_VERIFICATION
                      SET EXPIRESUTC = @p0
                      WHERE ACCOUNTID = @p1
                        AND CONSUMEDUTC IS NULL
                        AND EXPIRESUTC > @p0",
                    date, accountId);
            }
        }
    }
}
