using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;
using System.Collections.Generic;
using GuessWhoCore.Contracts.Faults;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServices.Coordinators;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Communication.Email.Builders.Context;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServerDomain.Domain.Enums.Security;

namespace GuessWhoTests.Services.Coordinators
{
    [TestClass]
    public class UserRegistrationManagerTests
    {
        private const string EMAIL = "nuevo@guesswho.com";
        private const string DISPLAY_NAME = "JugadorNuevo";
        private const string PASSWORD = "S3gur!dad2025";
        private const string AVATAR_ID = "DefaultAvatar";
        private const string PLAIN_CODE = "123456";
        private readonly byte[] HASH_CODE = new byte[] { 0, 1, 2 };
        private const long ACCOUNT_ID = 50;
        private const long USER_ID = 500;

        private Mock<IGuessWhoUnitOfWorkFactory> factoryMock;
        private Mock<IGuessWhoUnitOfWork> uowMock;
        private Mock<IUserAccountRepository> accountRepoMock;
        private Mock<IAvatarRepository> avatarRepoMock;
        private Mock<IEmailVerificationRepository> emailRepoMock;
        private Mock<IEmailSender> emailSenderMock;
        private Mock<IEmailMessageBuilder<VerificationCodeEmailContext>> builderMock;
        private Mock<IPasswordHasher> hasherMock;
        private Mock<IVerificationCodeService> codeServiceMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private TimeSpan lifeTime = TimeSpan.FromMinutes(10);
        private UserRegistrationManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            factoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            uowMock = new Mock<IGuessWhoUnitOfWork>();
            accountRepoMock = new Mock<IUserAccountRepository>();
            avatarRepoMock = new Mock<IAvatarRepository>();
            emailRepoMock = new Mock<IEmailVerificationRepository>();
            emailSenderMock = new Mock<IEmailSender>();
            builderMock = new Mock<IEmailMessageBuilder<VerificationCodeEmailContext>>();
            hasherMock = new Mock<IPasswordHasher>();
            codeServiceMock = new Mock<IVerificationCodeService>();
            transactionMock = new Mock<IGuessWhoDbTransaction>();

            factoryMock.Setup(f => f.Create()).Returns(uowMock.Object);
            uowMock.Setup(u => u.UserAccounts).Returns(accountRepoMock.Object);
            uowMock.Setup(u => u.Avatars).Returns(avatarRepoMock.Object);
            uowMock.Setup(u => u.EmailVerification).Returns(emailRepoMock.Object);
            uowMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);

            manager = new UserRegistrationManager(
                factoryMock.Object,
                emailSenderMock.Object,
                builderMock.Object,
                hasherMock.Object,
                codeServiceMock.Object,
                lifeTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new UserRegistrationManager(null, emailSenderMock.Object, builderMock.Object, hasherMock.Object, codeServiceMock.Object, lifeTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullEmailSender_ShouldThrowException()
        {
            new UserRegistrationManager(factoryMock.Object, null, builderMock.Object, hasherMock.Object, codeServiceMock.Object, lifeTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullMessageBuilder_ShouldThrowException()
        {
            new UserRegistrationManager(factoryMock.Object, emailSenderMock.Object, null, hasherMock.Object, codeServiceMock.Object, lifeTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullHasher_ShouldThrowException()
        {
            new UserRegistrationManager(factoryMock.Object, emailSenderMock.Object, builderMock.Object, null, codeServiceMock.Object, lifeTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullCodeService_ShouldThrowException()
        {
            new UserRegistrationManager(factoryMock.Object, emailSenderMock.Object, builderMock.Object, hasherMock.Object, null, lifeTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestConstructor_InvalidLifeTime_ShouldThrowException()
        {
            new UserRegistrationManager(factoryMock.Object, emailSenderMock.Object, builderMock.Object, hasherMock.Object, codeServiceMock.Object, TimeSpan.Zero);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_NullArgs_ShouldThrowFault()
        {
            manager.RegisterUser(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_DefaultNowUtc_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, default);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_EmailRequired_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(null, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_InvalidEmailFormat_ShouldThrowFault()
        {
            var args = new RegisterUserArgs("invalido", DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_DisplayNameRequired_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, null, PASSWORD, DateTime.UtcNow);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_DisplayNameTooLong_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, new string('A', 101), PASSWORD, DateTime.UtcNow);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_PasswordRequired_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, null, DateTime.UtcNow);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_AvatarNotConfigured_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            avatarRepoMock.Setup(r => r.GetDefaultAvatarId()).Returns(string.Empty);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_EmailExists_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            avatarRepoMock.Setup(r => r.GetDefaultAvatarId()).Returns(AVATAR_ID);
            accountRepoMock.Setup(r => r.EmailExists(EMAIL)).Returns(true);
            manager.RegisterUser(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestRegisterUser_TokenCreationFailed_ShouldThrowFault()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            avatarRepoMock.Setup(r => r.GetDefaultAvatarId()).Returns(AVATAR_ID);
            accountRepoMock.Setup(r => r.EmailExists(EMAIL)).Returns(false);
            codeServiceMock.Setup(s => s.CreateVerificationCodeOrFault()).Returns(new VerificationCodeResult(PLAIN_CODE, HASH_CODE));
            accountRepoMock.Setup(r => r.CreateAccount(It.IsAny<CreateAccountArgs>())).Returns(CreatedAccountResult.Ok(new AccountRecord { AccountId = ACCOUNT_ID }, new UserProfileRecord()));
            emailRepoMock.Setup(r => r.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(false);
            manager.RegisterUser(args);
        }

        [TestMethod]
        public void TestRegisterUser_Success_ShouldReturnRegisterResult()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            avatarRepoMock.Setup(r => r.GetDefaultAvatarId()).Returns(AVATAR_ID);
            accountRepoMock.Setup(r => r.EmailExists(EMAIL)).Returns(false);
            codeServiceMock.Setup(s => s.CreateVerificationCodeOrFault()).Returns(new VerificationCodeResult(PLAIN_CODE, HASH_CODE));
            accountRepoMock.Setup(r => r.CreateAccount(It.IsAny<CreateAccountArgs>())).Returns(CreatedAccountResult.Ok(new AccountRecord { AccountId = ACCOUNT_ID }, new UserProfileRecord { UserId = USER_ID, DisplayName = DISPLAY_NAME }));
            emailRepoMock.Setup(r => r.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(true);
            builderMock.Setup(b => b.Build(It.IsAny<VerificationCodeEmailContext>())).Returns(new EmailMessage(EMAIL, "Sub", "Body", true));
            emailSenderMock.Setup(s => s.Send(It.IsAny<EmailMessage>())).Returns(new EmailSendResult());

            var result = manager.RegisterUser(args);

            Assert.AreEqual(ACCOUNT_ID, result.AccountId);
        }

        [TestMethod]
        public void TestRegisterUser_EmailSendingFails_ShouldStillReturnSuccess()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            avatarRepoMock.Setup(r => r.GetDefaultAvatarId()).Returns(AVATAR_ID);
            accountRepoMock.Setup(r => r.EmailExists(EMAIL)).Returns(false);
            codeServiceMock.Setup(s => s.CreateVerificationCodeOrFault()).Returns(new VerificationCodeResult(PLAIN_CODE, HASH_CODE));
            accountRepoMock.Setup(r => r.CreateAccount(It.IsAny<CreateAccountArgs>())).Returns(CreatedAccountResult.Ok(new AccountRecord { AccountId = ACCOUNT_ID }, new UserProfileRecord { UserId = USER_ID }));
            emailRepoMock.Setup(r => r.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(true);
            emailSenderMock.Setup(s => s.Send(It.IsAny<EmailMessage>())).Returns(new EmailSendResult(EmailSendStatus.PermanentFailure, "ERR", "Fail"));

            var result = manager.RegisterUser(args);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestRegisterUser_EmailBuilderReturnsNull_ShouldStillReturnSuccess()
        {
            var args = new RegisterUserArgs(EMAIL, DISPLAY_NAME, PASSWORD, DateTime.UtcNow);
            avatarRepoMock.Setup(r => r.GetDefaultAvatarId()).Returns(AVATAR_ID);
            accountRepoMock.Setup(r => r.EmailExists(EMAIL)).Returns(false);
            codeServiceMock.Setup(s => s.CreateVerificationCodeOrFault()).Returns(new VerificationCodeResult(PLAIN_CODE, HASH_CODE));
            accountRepoMock.Setup(r => r.CreateAccount(It.IsAny<CreateAccountArgs>())).Returns(CreatedAccountResult.Ok(new AccountRecord { AccountId = ACCOUNT_ID }, new UserProfileRecord { UserId = USER_ID }));
            emailRepoMock.Setup(r => r.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(true);
            builderMock.Setup(b => b.Build(It.IsAny<VerificationCodeEmailContext>())).Returns((EmailMessage)null);

            var result = manager.RegisterUser(args);

            Assert.AreEqual(ACCOUNT_ID, result.AccountId);
        }
    }
}