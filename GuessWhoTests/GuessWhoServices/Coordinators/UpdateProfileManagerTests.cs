using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using GuessWhoCore.Contracts.Faults;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServices.Coordinators;

namespace GuessWhoTests.Services.Coordinators
{
    [TestClass]
    public class UpdateProfileManagerTests
    {
        private const long USER_ID = 10;
        private const long INVALID_USER_ID = 0;
        private const string DISPLAY_NAME = "PlayerOne";
        private const string NEW_NAME = "UpdatedPlayer";
        private const string EMAIL = "test@guesswho.com";
        private const string AVATAR = "A0001";
        private const string CURRENT_PWD = "CurrentPassword123!";
        private const string NEW_PWD = "NewPassword456!";

        private Mock<IGuessWhoUnitOfWorkFactory> unitOfWorkFactoryMock;
        private Mock<IGuessWhoUnitOfWork> unitOfWorkMock;
        private Mock<IUserAccountRepository> userRepoMock;
        private Mock<IPasswordHasher> passwordHasherMock;
        private Mock<IGuessWhoDbTransaction> transactionMock;
        private UpdateProfileManager manager;

        [TestInitialize]
        public void TestInitialize()
        {
            unitOfWorkFactoryMock = new Mock<IGuessWhoUnitOfWorkFactory>();
            unitOfWorkMock = new Mock<IGuessWhoUnitOfWork>();
            userRepoMock = new Mock<IUserAccountRepository>();
            passwordHasherMock = new Mock<IPasswordHasher>();
            transactionMock = new Mock<IGuessWhoDbTransaction>();

            unitOfWorkFactoryMock.Setup(f => f.Create()).Returns(unitOfWorkMock.Object);
            unitOfWorkMock.Setup(u => u.UserAccounts).Returns(userRepoMock.Object);
            unitOfWorkMock.Setup(u => u.BeginTransaction()).Returns(transactionMock.Object);

            manager = new UpdateProfileManager(unitOfWorkFactoryMock.Object, passwordHasherMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullFactory_ShouldThrowException()
        {
            new UpdateProfileManager(null, passwordHasherMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullHasher_ShouldThrowException()
        {
            new UpdateProfileManager(unitOfWorkFactoryMock.Object, null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestGetProfile_UserIdInvalid_ShouldThrowFault()
        {
            manager.GetProfile(INVALID_USER_ID);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestGetProfile_NotFound_ShouldThrowFault()
        {
            userRepoMock.Setup(r => r.GetAccountWithProfileByUserId(USER_ID)).Returns(AccountWithProfileResult.NotFound());

            manager.GetProfile(USER_ID);
        }

        [TestMethod]
        public void TestGetProfile_Success_ShouldReturnProfileSnapshot()
        {
            var account = new AccountRecord { Email = EMAIL, CreatedAtUtc = DateTime.UtcNow };
            var profile = new UserProfileRecord { DisplayName = DISPLAY_NAME, AvatarId = AVATAR };
            var result = AccountWithProfileResult.Found(account, profile);
            userRepoMock.Setup(r => r.GetAccountWithProfileByUserId(USER_ID)).Returns(result);

            var response = manager.GetProfile(USER_ID);

            Assert.IsNotNull(response);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdateUserProfile_NullRequest_ShouldThrowFault()
        {
            manager.UpdateUserProfile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdateUserProfile_NoChangesProvided_ShouldThrowFault()
        {
            var args = new UpdateProfileArgs(USER_ID, null, null, null, null, DateTime.UtcNow);

            manager.UpdateUserProfile(args);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestUpdateUserProfile_IncorrectCurrentPassword_ShouldThrowFault()
        {
            var args = new UpdateProfileArgs(USER_ID, null, CURRENT_PWD, NEW_PWD, null, DateTime.UtcNow);
            var account = new AccountRecord { PasswordHash = new byte[] { 1, 2 } };
            var loaded = AccountWithProfileResult.Found(account, new UserProfileRecord());

            userRepoMock.Setup(r => r.TryGetAccountWithProfileForUpdate(It.IsAny<AccountSearchParameters>())).Returns(loaded);
            passwordHasherMock.Setup(p => p.VerifyPassword(CURRENT_PWD, It.IsAny<byte[]>())).Returns(false);

            manager.UpdateUserProfile(args);
        }

        [TestMethod]
        public void TestUpdateUserProfile_Success_ShouldReturnUpdatedSnapshot()
        {
            var args = new UpdateProfileArgs(USER_ID, NEW_NAME, null, null, null, DateTime.UtcNow);
            var account = new AccountRecord { Email = EMAIL };
            var profile = new UserProfileRecord { DisplayName = DISPLAY_NAME };
            var loaded = AccountWithProfileResult.Found(account, profile);
            var updatedResult = UpdatedAccountResult.Ok(account, new UserProfileRecord { DisplayName = NEW_NAME });

            userRepoMock.Setup(r => r.TryGetAccountWithProfileForUpdate(It.IsAny<AccountSearchParameters>())).Returns(loaded);
            userRepoMock.Setup(r => r.UpdateDisplayNameAndPassword(It.IsAny<UpdateAccountArgs>())).Returns(updatedResult);

            var response = manager.UpdateUserProfile(args);

            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void TestDeleteUserProfile_Success_ShouldReturnTrue()
        {
            var result = AccountWithProfileResult.Found(new AccountRecord(), new UserProfileRecord());
            userRepoMock.Setup(r => r.GetAccountWithProfileByUserId(USER_ID)).Returns(result);
            userRepoMock.Setup(r => r.DeleteAccount(USER_ID, It.IsAny<DateTime>())).Returns(true);

            var response = manager.DeleteUserProfile(USER_ID);

            Assert.IsTrue(response);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestDeleteUserProfile_DeleteFailed_ShouldThrowFault()
        {
            var result = AccountWithProfileResult.Found(new AccountRecord(), new UserProfileRecord());
            userRepoMock.Setup(r => r.GetAccountWithProfileByUserId(USER_ID)).Returns(result);
            userRepoMock.Setup(r => r.DeleteAccount(USER_ID, It.IsAny<DateTime>())).Returns(false);

            manager.DeleteUserProfile(USER_ID);
        }
    }
}