using GuessWhoDataAccess.Data;
using GuessWhoDataAccess.Data.DataAccess.EmailVerification;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GuessWhoTests.Data.DataAccess.EmailVerification
{
    [TestClass]
    public class EmailVerificationTokenRecordMapperTests
    {
        private static readonly Guid DEFAULT_TOKEN_ID = Guid.NewGuid();
        private const long DEFAULT_ACCOUNT_ID = 1;
        private static readonly byte[] DEFAULT_CODE_HASH = new byte[] { 1, 2, 3 };
        private static readonly DateTime DEFAULT_CREATED_AT = DateTime.UtcNow;
        private static readonly DateTime DEFAULT_EXPIRES_AT = DateTime.UtcNow.AddMinutes(10);

        [TestMethod]
        public void TestToRecord_NullEntity_ReturnsInvalidRecord()
        {
            EMAIL_VERIFICATION entity = null;

            var resultRecord = EmailVerificationTokenRecordMapper.ToRecord(entity);

            Assert.IsFalse(resultRecord.IsValid);
        }

        [TestMethod]
        public void TestToRecord_ValidEntity_ReturnsMappedTokenId()
        {
            var entity = new EMAIL_VERIFICATION
            {
                TOKENID = DEFAULT_TOKEN_ID,
                ACCOUNTID = DEFAULT_ACCOUNT_ID,
                CODEHASH = DEFAULT_CODE_HASH,
                CREATEDATUTC = DEFAULT_CREATED_AT,
                EXPIRESUTC = DEFAULT_EXPIRES_AT,
                CONSUMEDUTC = null,
                FAILEDATTEMPTS = 0
            };

            var resultRecord = EmailVerificationTokenRecordMapper.ToRecord(entity);

            Assert.AreEqual(DEFAULT_TOKEN_ID, resultRecord.TokenId);
        }

        [TestMethod]
        public void TestToRecord_CodeHashNull_ReturnsEmptyArray()
        {
            var entity = new EMAIL_VERIFICATION
            {
                TOKENID = DEFAULT_TOKEN_ID,
                ACCOUNTID = DEFAULT_ACCOUNT_ID,
                CODEHASH = null,
                CREATEDATUTC = DEFAULT_CREATED_AT,
                EXPIRESUTC = DEFAULT_EXPIRES_AT,
                CONSUMEDUTC = null,
                FAILEDATTEMPTS = 0
            };

            var resultRecord = EmailVerificationTokenRecordMapper.ToRecord(entity);

            Assert.AreEqual(0, resultRecord.CodeHash.Length);
        }
    }
}
