using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using GuessWhoDataAccess.Data;
using GuessWhoDataAccess.Data.DataAccess.Accounts;
using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoTests.Infrastructure;
using GuessWhoTests.Infrastructure.Helpers;

namespace GuessWhoTests.Data.DataAccess.Accounts
{
    [TestClass]
    public class UserAccountDataTests : RepositoryTestBase
    {
        private const string DEFAULT_EMAIL = "test@test.com";
        private const string OTHER_EMAIL = "other@test.com";
        private const string DISPLAY_NAME = "UserTest";
        private const long DEFAULT_USER_ID = 1;
        private const long DEFAULT_ACCOUNT_ID = 50;
        private const long NEGATIVE_ID = -50;
        private const int INVALID_ID_CONST = -1;

        private UserAccountData userAccountRepository;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            userAccountRepository = new UserAccountData(MockContext.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullContext_ThrowsArgumentNullException()
        {
            new UserAccountData(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestGetAccountWithProfileForLogin_NullParameters_ThrowsArgumentNullException()
        {
            userAccountRepository.GetAccountWithProfileForLogin(null, DateTime.UtcNow);
        }

        [TestMethod]
        public void TestGetAccountWithProfileForLogin_Success_ReturnsSuccessStatus()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { EMAIL = DEFAULT_EMAIL, USERID = DEFAULT_USER_ID, ISDELETED = false }
            };

            var profiles = new List<USER_PROFILE>
            {
                new USER_PROFILE { USERID = DEFAULT_USER_ID, ISACTIVE = true }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateDbSet(profiles).Object);

            var result = userAccountRepository.GetAccountWithProfileForLogin(
                new AccountSearchParameters { Email = DEFAULT_EMAIL },
                DateTime.UtcNow);

            Assert.AreEqual(AccountProfileStatus.Success, result.Status);
        }

        [TestMethod]
        public void TestGetAccountWithProfileForLogin_EmailNotFound_ReturnsNotFoundOrDeleted()
        {
            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<ACCOUNT>().Object);

            var result = userAccountRepository.GetAccountWithProfileForLogin(
                new AccountSearchParameters { Email = OTHER_EMAIL },
                DateTime.UtcNow);

            Assert.AreEqual(AccountProfileStatus.NotFoundOrDeleted, result.Status);
        }

        [TestMethod]
        public void TestGetAccountWithProfileForLogin_DeletedAccount_ReturnsNotFoundOrDeleted()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { EMAIL = DEFAULT_EMAIL, ISDELETED = true }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            var result = userAccountRepository.GetAccountWithProfileForLogin(
                new AccountSearchParameters { Email = DEFAULT_EMAIL },
                DateTime.UtcNow);

            Assert.AreEqual(AccountProfileStatus.NotFoundOrDeleted, result.Status);
        }

        [TestMethod]
        public void TestGetAccountWithProfileForLogin_LockedAccount_ReturnsLocked()
        {
            var now = DateTime.UtcNow;

            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT
                {
                    EMAIL = DEFAULT_EMAIL,
                    LOCKEDUNTILUTC = now.AddMinutes(10),
                    ISDELETED = false
                }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            var result = userAccountRepository.GetAccountWithProfileForLogin(
                new AccountSearchParameters { Email = DEFAULT_EMAIL },
                now);

            Assert.AreEqual(AccountProfileStatus.Locked, result.Status);
        }

        [TestMethod]
        public void TestGetAccountWithProfileForLogin_ProfileNotFound_ReturnsProfileNotFound()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { EMAIL = DEFAULT_EMAIL, USERID = DEFAULT_USER_ID, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<USER_PROFILE>().Object);

            var result = userAccountRepository.GetAccountWithProfileForLogin(
                new AccountSearchParameters { Email = DEFAULT_EMAIL },
                DateTime.UtcNow);

            Assert.AreEqual(AccountProfileStatus.ProfileNotFound, result.Status);
        }

        [TestMethod]
        public void TestEmailExists_ExistingEmail_ReturnsTrue()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { EMAIL = DEFAULT_EMAIL, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.IsTrue(userAccountRepository.EmailExists(DEFAULT_EMAIL));
        }

        [TestMethod]
        public void TestCreateAccount_ValidArgs_ReturnsAccount()
        {
            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<ACCOUNT>().Object);

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<USER_PROFILE>().Object);

            var result = userAccountRepository.CreateAccount(
                new CreateAccountArgs
                {
                    Email = DEFAULT_EMAIL,
                    DisplayName = DISPLAY_NAME,
                    CreationDate = DateTime.UtcNow
                });

            Assert.IsNotNull(result.Account);
        }

        [TestMethod]
        public void TestGetAccountByIdAccount_ExistingAccount_ReturnsAccountId()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { ACCOUNTID = DEFAULT_ACCOUNT_ID, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.AreEqual(
                DEFAULT_ACCOUNT_ID,
                userAccountRepository.GetAccountByIdAccount(DEFAULT_ACCOUNT_ID).AccountId);
        }

        [TestMethod]
        public void TestGetAccountIdByEmail_ExistingEmail_ReturnsId()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT
                {
                    EMAIL = DEFAULT_EMAIL,
                    ACCOUNTID = DEFAULT_ACCOUNT_ID,
                    ISDELETED = false
                }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.AreEqual(
                DEFAULT_ACCOUNT_ID,
                userAccountRepository.GetAccountIdByEmail(DEFAULT_EMAIL));
        }

        [TestMethod]
        public void TestGetAccountIdByEmail_NullEmail_ReturnsInvalidId()
        {
            Assert.AreEqual(
                INVALID_ID_CONST,
                userAccountRepository.GetAccountIdByEmail(null));
        }

        [TestMethod]
        public void TestDeleteAccount_ExistingUser_ReturnsTrue()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { USERID = DEFAULT_USER_ID, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.IsTrue(
                userAccountRepository.DeleteAccount(DEFAULT_USER_ID, DateTime.UtcNow));
        }

        [TestMethod]
        public void TestUpdatePasswordOnly_ExistingAccount_ReturnsTrue()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { ACCOUNTID = DEFAULT_ACCOUNT_ID, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.IsTrue(
                userAccountRepository.UpdatePasswordOnly(
                    new UpdatePasswordArgs
                    {
                        AccountId = DEFAULT_ACCOUNT_ID,
                        NewPasswordHash = new byte[] { 1, 2 }
                    }));
        }

        [TestMethod]
        public void TestUpdateDisplayNameAndPassword_DeletedAccount_ReturnsFail()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { ACCOUNTID = DEFAULT_ACCOUNT_ID, ISDELETED = true }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.IsFalse(
                userAccountRepository.UpdateDisplayNameAndPassword(
                    new UpdateAccountArgs { AccountId = DEFAULT_ACCOUNT_ID }).IsSuccess);
        }

        [TestMethod]
        public void TestGetAccountWithProfileByUserId_NegativeId_ReturnsNotFound()
        {
            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<ACCOUNT>().Object);

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<USER_PROFILE>().Object);

            Assert.IsFalse(
                userAccountRepository.GetAccountWithProfileByUserId(NEGATIVE_ID).IsFound);
        }

        [TestMethod]
        public void TestUpdateDisplayNameAndPassword_InactiveProfile_ReturnsFailResult()
        {
            var accounts = new List<ACCOUNT>
    {
        new ACCOUNT
        {
            ACCOUNTID = DEFAULT_ACCOUNT_ID,
            USERID = DEFAULT_USER_ID,
            ISDELETED = false
        }
    };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<USER_PROFILE>().Object);

            Assert.IsFalse(
                userAccountRepository.UpdateDisplayNameAndPassword(
                    new UpdateAccountArgs { AccountId = DEFAULT_ACCOUNT_ID }).IsSuccess);
        }

        [TestMethod]
        public void TestUpdateDisplayNameAndPassword_VeryLongString_ReturnsFailResult()
        {
            var longName = new string('A', 5000);

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<ACCOUNT>().Object);

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateEmptyDbSet<USER_PROFILE>().Object);

            Assert.IsFalse(
                userAccountRepository.UpdateDisplayNameAndPassword(
                    new UpdateAccountArgs
                    {
                        AccountId = DEFAULT_ACCOUNT_ID,
                        NewDisplayName = longName
                    }).IsSuccess);
        }

        [TestMethod]
        public void TestMarkUserProfileInactive_ValidUserId_ReturnsTrue()
        {
            var profiles = new List<USER_PROFILE>
            {
                new USER_PROFILE { USERID = DEFAULT_USER_ID, ISACTIVE = true }
            };

            MockContext.Setup(c => c.USER_PROFILE)
                .Returns(RepositoryTestHelpers.CreateDbSet(profiles).Object);

            Assert.IsTrue(
                userAccountRepository.MarkUserProfileInactive(DEFAULT_USER_ID));
        }

        [TestMethod]
        public void TestUpdateLastLoginUtc_ValidEmail_ReturnsTrue()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { EMAIL = DEFAULT_EMAIL, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.IsTrue(
                userAccountRepository.UpdateLastLoginUtc(
                    new AccountSearchParameters { Email = DEFAULT_EMAIL },
                    DateTime.UtcNow));
        }

        [TestMethod]
        public void TestMarkEmailVerified_ExistingAccount_ReturnsTrue()
        {
            var accounts = new List<ACCOUNT>
            {
                new ACCOUNT { ACCOUNTID = DEFAULT_ACCOUNT_ID, ISDELETED = false }
            };

            MockContext.Setup(c => c.ACCOUNT)
                .Returns(RepositoryTestHelpers.CreateDbSet(accounts).Object);

            Assert.IsTrue(
                userAccountRepository.MarkEmailVerified(DEFAULT_ACCOUNT_ID, DateTime.UtcNow));
        }
    }
}
