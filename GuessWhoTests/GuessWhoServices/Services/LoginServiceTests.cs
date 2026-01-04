using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;
using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Request;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Models.Session;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Services;

namespace GuessWhoTests.Services
{
    [TestClass]
    public class LoginServiceTests
    {
        private const string VALID_EMAIL = "test@mail.com";
        private const string UPPER_EMAIL = "TEST@MAIL.COM";
        private const string VALID_PASSWORD = "123";
        private const string WRONG_PASSWORD = "wrong";
        private const string DISPLAY_NAME = "Claudio";
        private const int USER_ID = 1;

        private Mock<ILoginCoordinator> coordinatorMock;
        private LoginService service;

        [TestInitialize]
        public void TestInitialize()
        {
            coordinatorMock = new Mock<ILoginCoordinator>();
            service = new LoginService(coordinatorMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestConstructor_NullCoordinator_ShouldThrowException()
        {
            new LoginService(null);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLoginUser_NullRequest_ShouldThrowFaultException()
        {
            service.LoginUser(null);
        }

        [TestMethod]
        public void TestLoginUser_Success_ShouldReturnValidUserResponse()
        {
            LoginRequest request = new LoginRequest { Email = UPPER_EMAIL, Password = VALID_PASSWORD };
            UserProfileRecord profile = new UserProfileRecord { UserId = USER_ID, DisplayName = DISPLAY_NAME };
            AccountRecord account = new AccountRecord { Email = VALID_EMAIL };
            SessionLoginResult result = SessionLoginResult.CreateSuccessful(account, profile);

            coordinatorMock.Setup(c => c.LoginAndInitializeSession(It.IsAny<LoginArgs>()))
                .Returns(result);

            LoginResponse response = service.LoginUser(request);

            Assert.IsTrue(response.ValidUser);
        }

        [TestMethod]
        public void TestLoginUser_InvalidCredentials_ShouldReturnValidUserFalse()
        {
            LoginRequest request = new LoginRequest { Email = VALID_EMAIL, Password = WRONG_PASSWORD };
            SessionLoginResult result = SessionLoginResult.CreateFailed(LoginStatus.InvalidCredentials);

            coordinatorMock.Setup(c => c.LoginAndInitializeSession(It.IsAny<LoginArgs>()))
                .Returns(result);

            LoginResponse response = service.LoginUser(request);

            Assert.IsFalse(response.ValidUser);
        }

        [TestMethod]
        public void TestLoginUser_CoordinatorReturnsNull_ShouldReturnValidUserFalse()
        {
            LoginRequest request = new LoginRequest { Email = VALID_EMAIL, Password = VALID_PASSWORD };
            coordinatorMock.Setup(c => c.LoginAndInitializeSession(It.IsAny<LoginArgs>()))
                .Returns((SessionLoginResult)null);

            LoginResponse response = service.LoginUser(request);

            Assert.IsFalse(response.ValidUser);
        }

        [TestMethod]
        [ExpectedException(typeof(FaultException<ServiceFault>))]
        public void TestLogoutUser_NullRequest_ShouldThrowFaultException()
        {
            service.LogoutUser(null);
        }

        [TestMethod]
        public void TestLogoutUser_ValidRequest_ShouldReturnSuccess()
        {
            LogoutRequest request = new LogoutRequest { UserProfileId = USER_ID };
            coordinatorMock.Setup(c => c.Logout(USER_ID)).Returns(true);

            BasicResponse response = service.LogoutUser(request);

            Assert.IsTrue(response.Success);
        }

        [TestMethod]
        public void TestLogoutUser_CoordinatorFails_ShouldReturnFailureResponse()
        {
            LogoutRequest request = new LogoutRequest { UserProfileId = USER_ID };
            coordinatorMock.Setup(c => c.Logout(USER_ID)).Returns(false);

            BasicResponse response = service.LogoutUser(request);

            Assert.IsFalse(response.Success);
        }
    }
}