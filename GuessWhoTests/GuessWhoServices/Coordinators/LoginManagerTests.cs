using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;
using GuessWhoCore.Contracts.Faults;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServerDomain.Domain.Settings;
using GuessWhoServices.Coordinators;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServices.Coordinators.InternalDtos;

namespace GuessWhoTests.Services.Coordinators
{
    [TestClass]
    public class LoginManagerTests
    {
        private const string EMAIL = "test@guesswho.com";
        private const string PASSWORD = "S3gur!dad2025";
        private const string DUMMY_HASH = "AQIDBAUGBwg=";
        private const long ACCOUNT_ID = 1;

        private Mock<IGuessWhoUnitOfWorkFactory> unitOfWorkFactoryMock;
        private Mock<IGuessWhoUnitOfWork> unitOfWorkMock;
        private Mock<IPasswordHasher> passwordHasherMock;
        private UserSecuritySettings securitySettings;
        private LoginManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            unitOfWorkFactoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            unitOfWorkMock = new Mock<IGuessWhoUnitOfWork>();
            passwordHasherMock = new Mock<IPasswordHasher>();

            securitySettings = new UserSecuritySettings(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(2),
                3,
                "^[0-9]{6}$",
                60,
                5,
                DUMMY_HASH);

            unitOfWorkFactoryMock.Setup(f => f.Create()).Returns(unitOfWorkMock.Object);

            manager = new LoginManager(
                unitOfWorkFactoryMock.Object,
                passwordHasherMock.Object,
                securitySettings);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new LoginManager(null, passwordHasherMock.Object, securitySettings);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullSettings_ShouldThrowException()
        {
            new LoginManager(unitOfWorkFactoryMock.Object, passwordHasherMock.Object, null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLogin_NullArgs_ShouldThrowFault()
        {
            manager.Login(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLogin_EmptyEmail_ShouldThrowFault()
        {
            var args = new LoginArgs("", PASSWORD);

            manager.Login(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLogin_AccountNotFound_ShouldThrowFault()
        {
            var args = new LoginArgs(EMAIL, PASSWORD);
            var result = AccountProfileRecordResult.Fail(AccountProfileStatus.NotFoundOrDeleted);

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountWithProfileForLogin(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(result);

            manager.Login(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLogin_AccountLocked_ShouldThrowFault()
        {
            var args = new LoginArgs(EMAIL, PASSWORD);
            var result = AccountProfileRecordResult.Fail(AccountProfileStatus.Locked);

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountWithProfileForLogin(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(result);

            manager.Login(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLogin_InvalidPassword_ShouldThrowFault()
        {
            var args = new LoginArgs(EMAIL, PASSWORD);
            var account = new AccountRecord { PasswordHash = new byte[] { 1 } };
            var result = AccountProfileRecordResult.Ok(account, new UserProfileRecord());

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountWithProfileForLogin(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(result);
            passwordHasherMock.Setup(p => p.VerifyPassword(PASSWORD, It.IsAny<byte[]>())).Returns(false);

            manager.Login(args);
        }

        [TestMethod]
        public void TestLogin_ValidCredentials_ShouldReturnSuccessfulSession()
        {
            var args = new LoginArgs(EMAIL, PASSWORD);
            var account = new AccountRecord { AccountId = ACCOUNT_ID, Email = EMAIL };
            var result = AccountProfileRecordResult.Ok(account, new UserProfileRecord());

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountWithProfileForLogin(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(result);
            passwordHasherMock.Setup(p => p.VerifyPassword(PASSWORD, It.IsAny<byte[]>())).Returns(true);
            unitOfWorkMock.Setup(u => u.UserAccounts.UpdateLastLoginUtc(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(true);

            var sessionResult = manager.Login(args);

            Assert.IsTrue(sessionResult.IsSuccess);
        }

        [TestMethod]
        public void TestLogin_UpdateLastLoginFails_ShouldStillReturnSuccess()
        {
            var args = new LoginArgs(EMAIL, PASSWORD);
            var account = new AccountRecord { AccountId = ACCOUNT_ID, Email = EMAIL };
            var result = AccountProfileRecordResult.Ok(account, new UserProfileRecord());

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountWithProfileForLogin(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(result);
            passwordHasherMock.Setup(p => p.VerifyPassword(PASSWORD, It.IsAny<byte[]>())).Returns(true);
            unitOfWorkMock.Setup(u => u.UserAccounts.UpdateLastLoginUtc(It.IsAny<AccountSearchParameters>(), It.IsAny<DateTime>()))
                .Returns(false);

            var sessionResult = manager.Login(args);

            Assert.IsTrue(sessionResult.IsSuccess);
        }
    }
}