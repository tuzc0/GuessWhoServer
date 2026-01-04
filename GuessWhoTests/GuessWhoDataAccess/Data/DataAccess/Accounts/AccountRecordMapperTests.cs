using Microsoft.VisualStudio.TestTools.UnitTesting;
using GuessWhoDataAccess.Data;
using GuessWhoDataAccess.Data.DataAccess.Accounts;
using GuessWhoServerDomain.Domain.Models.Accounts;
using System;

namespace GuessWhoTests.Data.DataAccess.Accounts
{
    [TestClass]
    public class AccountRecordMapperTests
    {
        private const long DEFAULT_ACCOUNT_ID = 1;
        private const string EMPTY_STRING = "";

        [TestMethod]
        public void TestToAccountRecord_ValidEntity_ReturnsMappedAccountId()
        {
            var accountEntity = new ACCOUNT
            {
                ACCOUNTID = DEFAULT_ACCOUNT_ID
            };

            var resultRecord = AccountRecordMapper.ToAccountRecord(accountEntity);

            Assert.AreEqual(DEFAULT_ACCOUNT_ID, resultRecord.AccountId);
        }

        [TestMethod]
        public void TestToAccountRecord_NullEmailInEntity_ReturnsEmptyString()
        {
            var accountEntity = new ACCOUNT
            {
                EMAIL = null
            };

            var resultRecord = AccountRecordMapper.ToAccountRecord(accountEntity);

            Assert.AreEqual(EMPTY_STRING, resultRecord.Email);
        }

        [TestMethod]
        public void TestToAccountRecord_NullPasswordInEntity_ReturnsEmptyArray()
        {
            var accountEntity = new ACCOUNT
            {
                PASSWORD = null
            };

            var resultRecord = AccountRecordMapper.ToAccountRecord(accountEntity);

            Assert.AreEqual(0, resultRecord.PasswordHash.Length);
        }

        [TestMethod]
        public void TestToAccountRecord_NullEntity_ReturnsInvalidAccountId()
        {
            ACCOUNT accountEntity = null;

            var resultRecord = AccountRecordMapper.ToAccountRecord(accountEntity);

            Assert.AreEqual(AccountRecord.INVALID_ACCOUNT_ID, resultRecord.AccountId);
        }

        [TestMethod]
        public void TestToUserProfileRecord_NullEntity_ReturnsInvalidUserId()
        {
            USER_PROFILE profileEntity = null;

            var resultRecord = AccountRecordMapper.ToUserProfileRecord(profileEntity);

            Assert.AreEqual(UserProfileRecord.INVALID_USER_ID, resultRecord.UserId);
        }
    }
}