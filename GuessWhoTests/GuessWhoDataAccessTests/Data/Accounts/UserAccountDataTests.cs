using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWhoTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.Entity.Infrastructure;
using System.Text;

namespace GuessWhoTests.Integration.Data.DataAccess.Accounts
{
    [TestClass]
    public class UserAccountDataTests
    {
        private UserAccountData userAccountData;
        private const string SEEDED_DUPLICATE_EMAIL = "test@gmail.com";

        private const string MSG_ID_GENERATED = "The database should generate a valid ID greater than 0.";
        private const string MSG_PROFILE_LINKED = "The Account and Profile must share the same UserId.";
        private const string MSG_EMAIL_PERSISTED = "The persisted email must match the input email.";
        private const string MSG_EMAIL_EXISTS_TRUE = "EmailExists should return true for the seeded user test@gmail.com.";
        private const string MSG_EMAIL_EXISTS_FALSE = "Should return false for a non-existing email.";

        [TestInitialize]
        public void Setup()
        {
            DatabaseResetter.ResetDatabase();

            userAccountData = new UserAccountData();
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            DatabaseResetter.ResetDatabase();
        }

        [TestMethod]
        public void CreateAccount_ValidData_ShouldGenerateValidAccountId()
        {
            var args = CreateValidAccountArgs("validAccountidTest@gmail.com", "User_IdCheck");

            var result = userAccountData.CreateAccount(args);

            Assert.IsTrue(result.account.AccountId > 0, MSG_ID_GENERATED);
        }

        [TestMethod]
        public void CreateAccount_ValidData_ShouldLinkProfileToAccountCorrectly()
        {
            var args = CreateValidAccountArgs("validLinkProfileToAccountTest@gmail.com", "User_LinkCheck");

            var result = userAccountData.CreateAccount(args);

            Assert.AreEqual(result.account.UserId, result.profile.UserId, MSG_PROFILE_LINKED);
        }

        [TestMethod]
        public void CreateAccount_ValidData_ShouldPersistCorrectEmail()
        {
            string expectedEmail = "newUserTestPersist@gmail.com";
            var args = CreateValidAccountArgs(expectedEmail, "User_PersistCheck");

            var result = userAccountData.CreateAccount(args);

            Assert.AreEqual(expectedEmail, result.account.Email, MSG_EMAIL_PERSISTED);
        }

        [TestMethod]
        public void EmailExists_ExistingEmailTestAtGmailCom_ShouldReturnTrue()
        {
            string emailToCheck = SEEDED_DUPLICATE_EMAIL;

            bool emailExists = userAccountData.EmailExists(emailToCheck);

            Assert.IsTrue(emailExists, MSG_EMAIL_EXISTS_TRUE);
        }

        [TestMethod]
        public void CreateAccount_DuplicateEmail_ShouldThrowDbUpdateException()
        {
            var args = CreateValidAccountArgs(SEEDED_DUPLICATE_EMAIL, "User_Duplicate");

            Assert.ThrowsException<DbUpdateException>(() => userAccountData.CreateAccount(args));
        }

        [TestMethod]
        public void EmailExists_NonExistingEmail_ShouldReturnFalse()
        {
            string uniqueEmail = "NewUserUniqueForTest@test.com";

            bool emailExists = userAccountData.EmailExists(uniqueEmail);

            Assert.IsFalse(emailExists, MSG_EMAIL_EXISTS_FALSE);
        }

        private CreateAccountArgs CreateValidAccountArgs(string email, string displayName)
        {
            return new CreateAccountArgs
            {
                DisplayName = displayName,
                Email = email,
                Password = Encoding.UTF8.GetBytes("S3gur!dad2025"),
                CreationDate = DateTime.UtcNow
            };
        }
    }
}