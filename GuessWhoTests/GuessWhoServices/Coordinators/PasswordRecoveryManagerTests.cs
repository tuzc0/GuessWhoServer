using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Communication.Email.Builders.Context;
using GuessWhoServices.Coordinators;
using GuessWhoServices.Coordinators.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;

namespace GuessWhoTests.Coordinators
{
    [TestClass]
    public class PasswordRecoveryManagerTests
    {
        private const string VALID_EMAIL = "test@mail.com";
        private const string VALID_CODE = "123456";
        private const string NEW_PASSWORD = "NewPassword123";
        private const long VALID_ACCOUNT_ID = 10;
        private static readonly Guid VALID_TOKEN_ID = Guid.NewGuid();

        private Mock<IGuessWhoUnitOfWorkFactory> unitOfWorkFactoryMock;
        private Mock<IGuessWhoUnitOfWork> unitOfWorkMock;
        private Mock<IEmailSender> emailSenderMock;
        private Mock<IEmailMessageBuilder<VerificationCodeEmailContext>> emailBuilderMock;
        private Mock<IVerificationCodeService> verificationCodeServiceMock;
        private Mock<IEmailVerificationDomainService> emailDomainServiceMock;
        private Mock<IPasswordHasher> passwordHasherMock;
        private PasswordRecoveryManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            unitOfWorkFactoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            unitOfWorkMock = new Mock<IGuessWhoUnitOfWork>();
            emailSenderMock = new Mock<IEmailSender>();
            emailBuilderMock = new Mock<IEmailMessageBuilder<VerificationCodeEmailContext>>();
            verificationCodeServiceMock = new Mock<IVerificationCodeService>();
            emailDomainServiceMock = new Mock<IEmailVerificationDomainService>();
            passwordHasherMock = new Mock<IPasswordHasher>();

            unitOfWorkFactoryMock.Setup(f => f.Create()).Returns(unitOfWorkMock.Object);

            manager = new PasswordRecoveryManager(
                unitOfWorkFactoryMock.Object,
                emailSenderMock.Object,
                emailBuilderMock.Object,
                verificationCodeServiceMock.Object,
                emailDomainServiceMock.Object,
                passwordHasherMock.Object
            );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullUoWFactory_ShouldThrowException()
        {
            new PasswordRecoveryManager(null, emailSenderMock.Object, emailBuilderMock.Object, verificationCodeServiceMock.Object, emailDomainServiceMock.Object, passwordHasherMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSendRecoveryPassword_NullRequest_ShouldThrowFaultException()
        {
            manager.SendRecoveryPassword(null);
        }

        [TestMethod]
        public void TestSendRecoveryPassword_EmptyEmail_ShouldReturnAmbiguousSuccess()
        {
            var request = new PasswordRecoveryRequest { Email = " " };
            var response = manager.SendRecoveryPassword(request);
            Assert.IsTrue(response.Success);
        }

        [TestMethod]
        public void TestSendRecoveryPassword_AccountNotFound_ShouldReturnAmbiguousSuccess()
        {
            var request = new PasswordRecoveryRequest { Email = VALID_EMAIL };
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(It.IsAny<string>())).Returns(0);

            var response = manager.SendRecoveryPassword(request);
            Assert.IsTrue(response.Success);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSendRecoveryPassword_ResendLimitExceeded_ShouldThrowFaultException()
        {
            var request = new PasswordRecoveryRequest { Email = VALID_EMAIL };
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(It.IsAny<string>())).Returns(VALID_ACCOUNT_ID);
            emailDomainServiceMock.Setup(d => d.ValidateResendLimitsOrThrow(VALID_ACCOUNT_ID, It.IsAny<DateTime>()))
                .Throws(new FaultException<ServiceFault>(new ServiceFault()));

            manager.SendRecoveryPassword(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSendRecoveryPassword_TokenCreationFails_ShouldThrowFaultException()
        {
            var request = new PasswordRecoveryRequest { Email = VALID_EMAIL };
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(It.IsAny<string>())).Returns(VALID_ACCOUNT_ID);
            verificationCodeServiceMock.Setup(v => v.CreateVerificationCodeOrFault()).Returns(new VerificationCodeResult(VALID_CODE, new byte[0]));
            unitOfWorkMock.Setup(u => u.EmailVerification.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(false);

            manager.SendRecoveryPassword(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSendRecoveryPassword_EmailBuilderReturnsNull_ShouldThrowFaultException()
        {
            var request = new PasswordRecoveryRequest { Email = VALID_EMAIL };
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(It.IsAny<string>())).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(true);
            emailBuilderMock.Setup(b => b.Build(It.IsAny<VerificationCodeEmailContext>())).Returns((EmailMessage)null);

            manager.SendRecoveryPassword(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestSendRecoveryPassword_EmailSenderFails_ShouldThrowFaultException()
        {
            var request = new PasswordRecoveryRequest { Email = VALID_EMAIL };
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(It.IsAny<string>())).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>())).Returns(true);
            emailBuilderMock.Setup(b => b.Build(It.IsAny<VerificationCodeEmailContext>())).Returns(new EmailMessage(VALID_EMAIL, "Sub", "Body", true));
            emailSenderMock.Setup(s => s.Send(It.IsAny<EmailMessage>())).Returns(new EmailSendResult(EmailSendStatus.PermanentFailure, "ERR", "Fail"));

            manager.SendRecoveryPassword(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdatePasswordWithVerificationCode_NullRequest_ShouldThrowFaultException()
        {
            manager.UpdatePasswordWithVerificationCode(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdatePasswordWithVerificationCode_AccountNotFound_ShouldThrowFaultException()
        {
            var request = new UpdatePasswordRequest { Email = VALID_EMAIL };
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(It.IsAny<string>())).Returns(0);

            manager.UpdatePasswordWithVerificationCode(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdatePasswordWithVerificationCode_TokenInvalid_ShouldThrowFaultException()
        {
            var request = new UpdatePasswordRequest { Email = VALID_EMAIL };
            var invalidToken = EmailVerificationTokenRecord.CreateInvalid();
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(VALID_EMAIL)).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.GetLatestActiveTokenByAccountId(VALID_ACCOUNT_ID, It.IsAny<DateTime>())).Returns(invalidToken);

            manager.UpdatePasswordWithVerificationCode(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdatePasswordWithVerificationCode_ConsumeTokenFails_ShouldThrowFaultException()
        {
            var request = new UpdatePasswordRequest { Email = VALID_EMAIL, VerificationCode = VALID_CODE };
            var token = new EmailVerificationTokenRecord { TokenId = VALID_TOKEN_ID, AccountId = VALID_ACCOUNT_ID };
            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(new Mock<IGuessWhoDbTransaction>().Object);
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(VALID_EMAIL)).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.GetLatestActiveTokenByAccountId(VALID_ACCOUNT_ID, It.IsAny<DateTime>())).Returns(token);
            unitOfWorkMock.Setup(u => u.EmailVerification.ConsumeToken(VALID_TOKEN_ID)).Returns(0);

            manager.UpdatePasswordWithVerificationCode(request);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdatePasswordWithVerificationCode_UpdatePasswordDbFails_ShouldThrowFaultException()
        {
            var request = new UpdatePasswordRequest { Email = VALID_EMAIL, VerificationCode = VALID_CODE };
            var token = new EmailVerificationTokenRecord { TokenId = VALID_TOKEN_ID, AccountId = VALID_ACCOUNT_ID };
            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(new Mock<IGuessWhoDbTransaction>().Object);
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(VALID_EMAIL)).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.GetLatestActiveTokenByAccountId(VALID_ACCOUNT_ID, It.IsAny<DateTime>())).Returns(token);
            unitOfWorkMock.Setup(u => u.EmailVerification.ConsumeToken(VALID_TOKEN_ID)).Returns(1);
            unitOfWorkMock.Setup(u => u.UserAccounts.UpdatePasswordOnly(It.IsAny<UpdatePasswordArgs>())).Returns(false);

            manager.UpdatePasswordWithVerificationCode(request);
        }

        [TestMethod]
        public void TestUpdatePasswordWithVerificationCode_Success_ShouldReturnTrue()
        {
            var request = new UpdatePasswordRequest { Email = VALID_EMAIL, VerificationCode = VALID_CODE };
            var token = new EmailVerificationTokenRecord { TokenId = VALID_TOKEN_ID, AccountId = VALID_ACCOUNT_ID };
            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(new Mock<IGuessWhoDbTransaction>().Object);
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(VALID_EMAIL)).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.GetLatestActiveTokenByAccountId(VALID_ACCOUNT_ID, It.IsAny<DateTime>())).Returns(token);
            unitOfWorkMock.Setup(u => u.EmailVerification.ConsumeToken(VALID_TOKEN_ID)).Returns(1);
            unitOfWorkMock.Setup(u => u.UserAccounts.UpdatePasswordOnly(It.IsAny<UpdatePasswordArgs>())).Returns(true);

            bool result = manager.UpdatePasswordWithVerificationCode(request);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestUpdatePasswordWithVerificationCode_Success_ShouldCommitTransaction()
        {
            var request = new UpdatePasswordRequest { Email = VALID_EMAIL, VerificationCode = VALID_CODE };
            var token = new EmailVerificationTokenRecord { TokenId = VALID_TOKEN_ID, AccountId = VALID_ACCOUNT_ID };
            var transactionMock = new Mock<IGuessWhoDbTransaction>();
            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);
            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountIdByEmail(VALID_EMAIL)).Returns(VALID_ACCOUNT_ID);
            unitOfWorkMock.Setup(u => u.EmailVerification.GetLatestActiveTokenByAccountId(VALID_ACCOUNT_ID, It.IsAny<DateTime>())).Returns(token);
            unitOfWorkMock.Setup(u => u.EmailVerification.ConsumeToken(VALID_TOKEN_ID)).Returns(1);
            unitOfWorkMock.Setup(u => u.UserAccounts.UpdatePasswordOnly(It.IsAny<UpdatePasswordArgs>())).Returns(true);

            manager.UpdatePasswordWithVerificationCode(request);

            transactionMock.Verify(t => t.Commit(), Times.Once);
        }
    }
}