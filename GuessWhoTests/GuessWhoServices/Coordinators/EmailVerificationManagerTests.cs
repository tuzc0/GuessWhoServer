using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Coordinators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;

namespace GuessWhoTests.Services.Coordinators
{
    [TestClass]
    public class EmailVerificationManagerTests
    {
        private const long ACCOUNT_ID = 1;
        private const string EMAIL = "test@email.com";
        private const string CODE = "123456";

        private Mock<IGuessWhoUnitOfWorkFactory> unitOfWorkFactoryMock;
        private Mock<IGuessWhoUnitOfWork> unitOfWorkMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private Mock<IVerificationCodeService> verificationCodeServiceMock;
        private Mock<IEmailSender> emailSenderMock;
        private Mock<IEmailVerificationDomainService> domainServiceMock;

        private VerificationCodeEmailBuilder emailBuilder;
        private EmailVerificationManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            unitOfWorkFactoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            unitOfWorkMock = new Mock<IGuessWhoUnitOfWork>();
            transactionMock = new Mock<IGuessWhoDbTransaction>();
            verificationCodeServiceMock = new Mock<IVerificationCodeService>();
            emailSenderMock = new Mock<IEmailSender>();
            domainServiceMock = new Mock<IEmailVerificationDomainService>();

            emailBuilder = new VerificationCodeEmailBuilder();

            unitOfWorkFactoryMock
                .Setup(f => f.Create())
                .Returns(unitOfWorkMock.Object);

            unitOfWorkMock
                .Setup(u => u.BeginTransaction())
                .Returns(transactionMock.Object);

            manager = new EmailVerificationManager(
                unitOfWorkFactoryMock.Object,
                verificationCodeServiceMock.Object,
                emailSenderMock.Object,
                emailBuilder,
                domainServiceMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void ConfirmEmail_NullRequest_ThrowsFault()
        {
            manager.ConfirmEmailAddressWithVerificationCode(null);
        }

        [TestMethod]
        public void ConfirmEmail_AlreadyVerifiedAccount_ReturnsSuccess()
        {
            var request = new VerifyEmailRequest
            {
                AccountId = ACCOUNT_ID,
                Code = CODE
            };

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountByIdAccount(ACCOUNT_ID))
                .Returns(new AccountRecord
                {
                    AccountId = ACCOUNT_ID,
                    IsEmailVerified = true
                });

            var response = manager.ConfirmEmailAddressWithVerificationCode(request);

            Assert.IsTrue(response.Success);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void ConfirmEmail_NoActiveToken_ThrowsFault()
        {
            var request = new VerifyEmailRequest
            {
                AccountId = ACCOUNT_ID,
                Code = CODE
            };

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountByIdAccount(ACCOUNT_ID))
                .Returns(new AccountRecord
                {
                    AccountId = ACCOUNT_ID,
                    IsEmailVerified = false
                });

            unitOfWorkMock.Setup(u =>
                u.EmailVerification.GetLatestActiveTokenByAccountId(
                    ACCOUNT_ID,
                    It.IsAny<DateTime>()))
                .Returns((EmailVerificationTokenRecord)null);

            manager.ConfirmEmailAddressWithVerificationCode(request);
        }

        [TestMethod]
        public void ResendEmail_AlreadyVerifiedAccount_DoesNothing()
        {
            var request = new ResendVerificationRequest
            {
                AccountId = ACCOUNT_ID
            };

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountByIdAccount(ACCOUNT_ID))
                .Returns(new AccountRecord
                {
                    AccountId = ACCOUNT_ID,
                    IsEmailVerified = true
                });

            manager.ResendEmailVerificationCode(request);

            Assert.IsTrue(true);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void ResendEmail_TokenCreationFails_ThrowsFault()
        {
            var request = new ResendVerificationRequest
            {
                AccountId = ACCOUNT_ID
            };

            unitOfWorkMock.Setup(u => u.UserAccounts.GetAccountByIdAccount(ACCOUNT_ID))
                .Returns(new AccountRecord
                {
                    AccountId = ACCOUNT_ID,
                    Email = EMAIL,
                    IsEmailVerified = false
                });

            domainServiceMock
                .Setup(d => d.ValidateResendLimitsOrThrow(ACCOUNT_ID, It.IsAny<DateTime>()));

            verificationCodeServiceMock
                .Setup(v => v.CreateVerificationCodeOrFault())
                .Returns(new VerificationCodeResult(CODE, new byte[] { 1 }));

            domainServiceMock
                .Setup(d => d.GetVerificationCodeLifetime())
                .Returns(TimeSpan.FromMinutes(5));

            unitOfWorkMock
                .Setup(u => u.EmailVerification.AddVerificationToken(It.IsAny<CreateEmailTokenArgs>()))
                .Returns(false);

            manager.ResendEmailVerificationCode(request);
        }
    }
}
