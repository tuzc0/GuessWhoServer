using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using System;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification
{
    public sealed class EmailVerificationData
    {

        public EMAIL_VERIFICATION AddVerificationToken(CreateEmailTokenArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var token = new EMAIL_VERIFICATION
                {
                    ACCOUNTID = args.AccountId,
                    CODEHASH = args.CodeHash,
                    CREATEDATUTC = args.NowUtc,
                    EXPIRESUTC = args.NowUtc.Add(args.LifeSpan),
                    CONSUMEDUTC = null
                };

                dataBaseContext.EMAIL_VERIFICATION.Add(token);
                dataBaseContext.SaveChanges();

                return token;
            }
        }

        public EMAIL_VERIFICATION GetLatestTokenByAccountId(long accountId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == accountId && t.CONSUMEDUTC == null)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .FirstOrDefault();
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

        public (bool IsPerMinuteCooldownActive, bool IsWithinHourlyLimit, DateTime? LastTokenCreatedAtUtc,
            int TokensSentInLastHour) GetEmailVerificationResendLimits(long accountId, DateTime date)
        {

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var lastTokenCreatedAtUtc = dataBaseContext.EMAIL_VERIFICATION
                    .Where(t => t.ACCOUNTID == accountId)
                    .OrderByDescending(t => t.CREATEDATUTC)
                    .Select(t => (DateTime?)t.CREATEDATUTC)
                    .FirstOrDefault();

                var oneHourAgoUtc = date.AddHours(-1);

                var tokensSentInLastHour = dataBaseContext.EMAIL_VERIFICATION
                    .Count(t => t.ACCOUNTID == accountId && t.CREATEDATUTC >= oneHourAgoUtc);

                var isPerMinuteCooldownActive =lastTokenCreatedAtUtc.HasValue &&
                    (date - lastTokenCreatedAtUtc.Value).TotalSeconds < 60;

                var isWithinHourlyLimit = tokensSentInLastHour < 5;

                return (isPerMinuteCooldownActive, isWithinHourlyLimit,
                    lastTokenCreatedAtUtc, tokensSentInLastHour);
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
