using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWhoTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GuessWhoTests.Integration.Data.DataAccess.EmailVerification
{
    [TestClass]
    public class EmailVerificationDataTests
    {
        private EmailVerificationData emailVerificationData;


        //REFACTOR ::::::::::::::

        private const long SEEDED_ACCOUNT_ID = 1;
        private const long NON_EXISTENT_ACCOUNT_ID = 9999;
        private static readonly byte[] VALID_CODE_HASH = { 0x01, 0x02, 0x03, 0x04 };
        private static readonly TimeSpan VALID_LIFESPAN = TimeSpan.FromMinutes(10);
        private const int MAX_ATTEMPTS = 5;

        private const string MSG_TOKEN_NOT_NULL = "The returned token object must not be null.";
        private const string MSG_TOKEN_NULL = "Should return null when no valid token is found.";
        private const string MSG_ACCOUNTID_MATCH = "The stored AccountId must match the input AccountId.";
        private const string MSG_EXPIRES_MATCH = "The token expiration must match the calculated expiration time.";
        private const string MSG_CONSUMED_NULL = "The ConsumedAtUtc field must be null upon creation.";
        private const string MSG_FAILED_ATTEMPTS_ZERO = "The FailedAttempts count must be zero upon creation.";
        private const string MSG_LATEST_TOKEN_FOUND = "The latest active token must be returned.";
        private const string MSG_ROWS_UPDATED = "Must update exactly one row.";
        private const string MSG_NO_ROWS_UPDATED = "Must update zero rows when token is invalid or consumed.";
        private const string MSG_ATTEMPTS_INCREMENTED = "Failed attempts must be incremented by 1.";
        private const string MSG_TOKEN_EXPIRED_TIME = "The token Expiration time must be updated (expired).";
        private const string MSG_COOLDOWN_INACTIVE = "IsPerMinuteCooldownActive must be false.";
        private const string MSG_LIMIT_REACHED = "IsWithinHourlyLimit must be false when limit is reached.";
        private const string MSG_LIMIT_NOT_REACHED = "IsWithinHourlyLimit must be true when limit is not reached.";
        private const string MSG_CONSUMED_SET = "The token must be marked as consumed.";


        [TestInitialize]
        public void Setup()
        {
            DatabaseResetter.ResetDatabase();
            emailVerificationData = new EmailVerificationData();
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            DatabaseResetter.ResetDatabase();
        }

        private CreateEmailTokenArgs CreateValidArgs(DateTime now, TimeSpan lifeSpan)
        {
            return new CreateEmailTokenArgs
            {
                AccountId = SEEDED_ACCOUNT_ID,
                CodeHash = VALID_CODE_HASH,
                NowUtc = now,
                LifeSpan = lifeSpan
            };
        }

        private EMAIL_VERIFICATION CreateTokenWithInitialAttempts(DateTime truncatedNow, int initialAttempts)
        {
            var token = new EMAIL_VERIFICATION
            {
                ACCOUNTID = SEEDED_ACCOUNT_ID,
                CODEHASH = VALID_CODE_HASH,
                CREATEDATUTC = truncatedNow.AddMinutes(-10),
                EXPIRESUTC = truncatedNow.AddMinutes(10),
                FAILEDATTEMPTS = initialAttempts,
                CONSUMEDUTC = null
            };

            using (var db = new GuessWhoDBEntities())
            {
                db.EMAIL_VERIFICATION.Add(token);
                db.SaveChanges();
            }
            return token;
        }


        private void AssertTokenAttemptsAre(Guid tokenId, int expectedAttempts, string message)
        {
            using (var db = new GuessWhoDBEntities())
            {
                var updatedToken = db.EMAIL_VERIFICATION.Single(t => t.TOKENID == tokenId);
                Assert.AreEqual(expectedAttempts, updatedToken.FAILEDATTEMPTS, message);
            }
        }

        private void AssertTokenIsConsumed(Guid tokenId, string message)
        {
            using (var db = new GuessWhoDBEntities())
            {
                var consumedToken = db.EMAIL_VERIFICATION.Single(t => t.TOKENID == tokenId);
                Assert.IsNotNull(consumedToken.CONSUMEDUTC, message);
            }
        }

        private void AssertTokenExpirationIsSetToNow(Guid tokenId, DateTime expectedExpirationUtc, string failureMessage)
        {
            using (var db = new GuessWhoDBEntities())
            {
                var expiredToken = db.EMAIL_VERIFICATION.SingleOrDefault(t => t.TOKENID == tokenId);

                Assert.IsNotNull(expiredToken, "Token was not found in the database after expiration attempt.");

                TimeSpan timeDifference = expiredToken.EXPIRESUTC - expectedExpirationUtc;

                Assert.IsTrue(Math.Abs(timeDifference.TotalSeconds) < 1, failureMessage);
            }
        }

        [TestMethod]
        public void AddVerificationToken_ValidArgs_ShouldReturnNotNullToken()
        {
            DateTime now = DateTime.UtcNow;
            var args = CreateValidArgs(now, VALID_LIFESPAN);

            var token = emailVerificationData.AddVerificationToken(args);

            Assert.IsNotNull(token, MSG_TOKEN_NOT_NULL);
        }

        [TestMethod]
        public void AddVerificationToken_ValidArgs_ShouldPersistCorrectAccountId()
        {
            DateTime now = DateTime.UtcNow;
            var args = CreateValidArgs(now, VALID_LIFESPAN);

            var token = emailVerificationData.AddVerificationToken(args);

            Assert.AreEqual(SEEDED_ACCOUNT_ID, token.ACCOUNTID, MSG_ACCOUNTID_MATCH);
        }

        [TestMethod]
        public void AddVerificationToken_ValidArgs_ShouldPersistCorrectExpirationTime()
        {
            DateTime now = DateTime.UtcNow;
            var args = CreateValidArgs(now, VALID_LIFESPAN);
            DateTime expectedExpiration = now.Add(VALID_LIFESPAN);

            var token = emailVerificationData.AddVerificationToken(args);

            Assert.AreEqual(expectedExpiration, token.EXPIRESUTC, MSG_EXPIRES_MATCH);
        }

        [TestMethod]
        public void AddVerificationToken_ValidArgs_ShouldPersistConsumedAsNull()
        {
            DateTime now = DateTime.UtcNow;
            var args = CreateValidArgs(now, VALID_LIFESPAN);

            var token = emailVerificationData.AddVerificationToken(args);

            Assert.IsNull(token.CONSUMEDUTC, MSG_CONSUMED_NULL);
        }

        [TestMethod]
        public void AddVerificationToken_ValidArgs_ShouldPersistFailedAttemptsAsZero()
        {
            DateTime now = DateTime.UtcNow;
            var args = CreateValidArgs(now, VALID_LIFESPAN);

            var token = emailVerificationData.AddVerificationToken(args);

            Assert.AreEqual(0, token.FAILEDATTEMPTS, MSG_FAILED_ATTEMPTS_ZERO);
        }

        [TestMethod]
        public void ConsumeToken_ValidToken_ShouldReturnOneRowUpdated()
        {
            DateTime now = DateTime.UtcNow;
            var token = emailVerificationData.AddVerificationToken(CreateValidArgs(now, VALID_LIFESPAN));

            int rows = emailVerificationData.ConsumeToken(token.TOKENID);

            Assert.AreEqual(1, rows, MSG_ROWS_UPDATED);
        }

        [TestMethod]
        public void ConsumeToken_ValidToken_ShouldMarkAsConsumed()
        {
            DateTime now = DateTime.UtcNow;
            var token = emailVerificationData.AddVerificationToken(CreateValidArgs(now, VALID_LIFESPAN));

            emailVerificationData.ConsumeToken(token.TOKENID);

            AssertTokenIsConsumed(token.TOKENID, MSG_CONSUMED_SET);
        }

        [TestMethod]
        public void ConsumeToken_AlreadyConsumed_ShouldReturnZeroRows()
        {
            DateTime now = DateTime.UtcNow;
            var token = emailVerificationData.AddVerificationToken(CreateValidArgs(now, VALID_LIFESPAN));
            emailVerificationData.ConsumeToken(token.TOKENID);

            int rows = emailVerificationData.ConsumeToken(token.TOKENID);

            Assert.AreEqual(0, rows, MSG_NO_ROWS_UPDATED);
        }

        [TestMethod]
        public void IncrementFailedAttempts_NotMaxAttempts_ShouldIncrement()
        {
            DateTime now = DateTime.UtcNow;
            var token = emailVerificationData.AddVerificationToken(CreateValidArgs(now, VALID_LIFESPAN));

            emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(token.TOKENID, now, MAX_ATTEMPTS);

            AssertTokenAttemptsAre(token.TOKENID, 1, MSG_ATTEMPTS_INCREMENTED);
        }

        [TestMethod]
        public void IncrementFailedAttempts_AtMaxAttempts_ShouldExpireToken()
        {
            DateTime now = DateTime.UtcNow;
            DateTime truncatedNow = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind);

            var token = CreateTokenWithInitialAttempts(truncatedNow, MAX_ATTEMPTS - 1);

            emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(token.TOKENID, truncatedNow, MAX_ATTEMPTS);

            AssertTokenExpirationIsSetToNow(token.TOKENID, truncatedNow, MSG_TOKEN_EXPIRED_TIME);
        }

        [TestMethod]
        public void IncrementFailedAttempts_ValidToken_ShouldReturnOneRowUpdated()
        {
            DateTime now = DateTime.UtcNow;
            var token = emailVerificationData.AddVerificationToken(CreateValidArgs(now, VALID_LIFESPAN));

            int rows = emailVerificationData.IncrementFailedAttemptsAndMaybeExpire(token.TOKENID, now, MAX_ATTEMPTS);

            Assert.AreEqual(1, rows, MSG_ROWS_UPDATED);
        }

        [TestMethod]
        public void ExpireActiveTokens_ActiveToken_ShouldExpireExpirationTime()
        {
            DateTime now = DateTime.UtcNow;
            var tokenArgs = CreateValidArgs(now.AddMinutes(-10), VALID_LIFESPAN);
            var uniqueToken = emailVerificationData.AddVerificationToken(tokenArgs);

            emailVerificationData.ExpireActiveTokens(SEEDED_ACCOUNT_ID, now);

            AssertTokenExpirationIsSetToNow(uniqueToken.TOKENID, now, MSG_TOKEN_EXPIRED_TIME);
        }

        [TestMethod]
        public void GetEmailVerificationResendLimits_NoTokens_ShouldReturnCooldownInactive()
        {
            DateTime now = DateTime.UtcNow;

            var (isPerMinuteCooldownActive, _, _, _) = emailVerificationData.GetEmailVerificationResendLimits(SEEDED_ACCOUNT_ID, now);

            Assert.IsFalse(isPerMinuteCooldownActive, MSG_COOLDOWN_INACTIVE);
        }

        [TestMethod]
        public void GetEmailVerificationResendLimits_InLastMinute_ShouldReturnCooldownActive()
        {
            DateTime now = DateTime.UtcNow;
            emailVerificationData.AddVerificationToken(CreateValidArgs(now.AddSeconds(-30), VALID_LIFESPAN));

            var (isPerMinuteCooldownActive, _, _, _) = emailVerificationData.GetEmailVerificationResendLimits(SEEDED_ACCOUNT_ID, now);

            Assert.IsTrue(isPerMinuteCooldownActive, "IsPerMinuteCooldownActive must be true.");
        }

        [TestMethod]
        public void GetEmailVerificationResendLimits_OverOneMinuteAgo_ShouldReturnCooldownInactive()
        {
            DateTime now = DateTime.UtcNow;
            emailVerificationData.AddVerificationToken(CreateValidArgs(now.AddSeconds(-61), VALID_LIFESPAN));

            var (isPerMinuteCooldownActive, _, _, _) = emailVerificationData.GetEmailVerificationResendLimits(SEEDED_ACCOUNT_ID, now);

            Assert.IsFalse(isPerMinuteCooldownActive, MSG_COOLDOWN_INACTIVE);
        }

        [TestMethod]
        public void GetEmailVerificationResendLimits_UnderHourlyLimit_ShouldReturnLimitNotReached()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan oneHour = TimeSpan.FromHours(1);

            for (int i = 1; i <= 4; i++)
            {
                emailVerificationData.AddVerificationToken(CreateValidArgs(now.AddMinutes(-i), oneHour));
            }

            var (_, isWithinHourlyLimit, _, tokensSent) = emailVerificationData.GetEmailVerificationResendLimits(SEEDED_ACCOUNT_ID, now);

            Assert.IsTrue(isWithinHourlyLimit, MSG_LIMIT_NOT_REACHED);
        }

        [TestMethod]
        public void GetEmailVerificationResendLimits_AtHourlyLimit_ShouldReturnLimitReached()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan oneHour = TimeSpan.FromHours(1);

            for (int i = 1; i <= 5; i++)
            {
                emailVerificationData.AddVerificationToken(CreateValidArgs(now.AddMinutes(-i), oneHour));
            }

            var (_, isWithinHourlyLimit, _, tokensSent) = emailVerificationData.GetEmailVerificationResendLimits(SEEDED_ACCOUNT_ID, now);

            Assert.IsFalse(isWithinHourlyLimit, MSG_LIMIT_REACHED);
        }
    }
}