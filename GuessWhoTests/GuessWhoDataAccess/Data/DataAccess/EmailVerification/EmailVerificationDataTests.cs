using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using GuessWhoDataAccess.Data;
using GuessWhoDataAccess.Data.DataAccess.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoTests.Infrastructure;
using GuessWhoTests.Infrastructure.Helpers;

namespace GuessWhoTests.Data.DataAccess.EmailVerification
{
    [TestClass]
    public class EmailVerificationDataTests : RepositoryTestBase
    {
        private const long DEFAULT_ACCOUNT_ID = 1;
        private const int DEFAULT_COOLDOWN_SECONDS = 60;
        private const int DEFAULT_HOURLY_LIMIT = 5;

        private EmailVerificationData emailVerificationRepository;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            emailVerificationRepository = new EmailVerificationData(MockContext.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullContext_ThrowsArgumentNullException()
        {
            new EmailVerificationData(null);
        }

        [TestMethod]
        public void TestAddVerificationToken_ValidArgs_ReturnsTrue()
        {
            MockContext.Setup(c => c.EMAIL_VERIFICATION)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<EMAIL_VERIFICATION>().Object);

            var args = new CreateEmailTokenArgs
            {
                AccountId = DEFAULT_ACCOUNT_ID,
                CodeHash = new byte[] { 1 },
                NowUtc = DateTime.UtcNow,
                LifeSpan = TimeSpan.FromMinutes(10)
            };

            var result = emailVerificationRepository.AddVerificationToken(args);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestGetLatestActiveTokenByAccountId_NoTokens_ReturnsInvalidRecord()
        {
            MockContext.Setup(c => c.EMAIL_VERIFICATION)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<EMAIL_VERIFICATION>().Object);

            var result = emailVerificationRepository.GetLatestActiveTokenByAccountId(DEFAULT_ACCOUNT_ID, DateTime.UtcNow);

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void TestGetLatestTokenStatusByAccountId_NoTokens_ReturnsInvalidRecord()
        {
            MockContext.Setup(c => c.EMAIL_VERIFICATION)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<EMAIL_VERIFICATION>().Object);

            var result = emailVerificationRepository.GetLatestTokenStatusByAccountId(DEFAULT_ACCOUNT_ID);

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void TestGetEmailVerificationResendLimits_NoTokens_ReturnsWithinLimits()
        {
            MockContext.Setup(c => c.EMAIL_VERIFICATION)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<EMAIL_VERIFICATION>().Object);

            var query = new ResendLimitsQuery(DEFAULT_ACCOUNT_ID, DateTime.UtcNow, DEFAULT_COOLDOWN_SECONDS, DEFAULT_HOURLY_LIMIT);

            var result = emailVerificationRepository.GetEmailVerificationResendLimits(query);

            Assert.IsTrue(result.IsWithinHourlyLimit);
        }
    }
}
