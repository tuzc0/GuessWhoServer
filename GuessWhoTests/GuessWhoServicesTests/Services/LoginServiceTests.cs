using GuessWho.Services.WCF.Services;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;

namespace GuessWhoTests.GuessWhoServicesTests.Services
{
    [TestClass]
    public class LoginServiceTests
    {
        private LoginService loginService;

        private const string SEEDED_VALID_EMAIL = "test@gmail.com";
        private const string SEEDED_VALID_PASSWORD = "12345678#a";
        private const int SEEDED_VALID_USER_ID = 5;

        private const string MSG_REQUEST_NULL = "Should throw FAULT_CODE_REQUEST_NULL when request is null.";
        private const string MSG_CREDENTIALS_REQUIRED = "Should throw FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED when credentials are missing.";
        private const string MSG_USER_NOT_FOUND = "Should return ValidUser = false when user email is not found.";
        private const string MSG_INVALID_PASSWORD = "Should throw FAULT_CODE_LOGIN_INVALID_PASSWORD when password is incorrect.";
        private const string MSG_SUCCESSFUL_LOGIN = "Should return ValidUser = true on successful login.";
        private const string MSG_USER_ID_MATCH = "The returned UserId must match the expected seeded UserId.";
        private const string MSG_FAULT_CODE_MATCH = "FaultCode should match expected value.";
        private const string MSG_USER_ID_INVALID_RESPONSE = "UserId should be -1 for an invalid user.";

        [TestInitialize]
        public void Setup()
        {
            DatabaseResetter.ResetDatabase();
            loginService = new LoginService();
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            DatabaseResetter.ResetDatabase();
        }

        [TestMethod]
        public void LoginUser_NullRequest_ShouldThrowFaultException()
        {
            Assert.ThrowsException<FaultException<ServiceFault>>(() => loginService.LoginUser(null), MSG_REQUEST_NULL);
        }

        [TestMethod]
        public void LoginUser_NullRequest_ShouldHaveCorrectFaultCode()
        {
            string actualCode = string.Empty;

            try
            {
                loginService.LoginUser(null);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }

            Assert.AreEqual("REQUEST_NULL", actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void LoginUser_MissingEmail_ShouldThrowCredentialsRequiredFault()
        {
            var request = new LoginRequest { User = "", Password = "AnyPassword" };

            Assert.ThrowsException<FaultException<ServiceFault>>(() => loginService.LoginUser(request), MSG_CREDENTIALS_REQUIRED);
        }

        [TestMethod]
        public void LoginUser_MissingEmail_ShouldHaveCorrectFaultCode()
        {
            var request = new LoginRequest { User = "", Password = "AnyPassword" };
            string actualCode = string.Empty;

            try
            {
                loginService.LoginUser(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }

            Assert.AreEqual("LOGIN_CREDENTIALS_REQUIRED", actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void LoginUser_MissingPassword_ShouldThrowCredentialsRequiredFault()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = "" };

            Assert.ThrowsException<FaultException<ServiceFault>>(() => loginService.LoginUser(request), MSG_CREDENTIALS_REQUIRED);
        }

        [TestMethod]
        public void LoginUser_MissingPassword_ShouldHaveCorrectFaultCode()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = "" };
            string actualCode = string.Empty;

            try
            {
                loginService.LoginUser(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }

            Assert.AreEqual("LOGIN_CREDENTIALS_REQUIRED", actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void LoginUser_UserNotFound_ShouldReturnValidUserFalse()
        {
            var request = new LoginRequest { User = "nonexistent@test.com", Password = SEEDED_VALID_PASSWORD };
            LoginResponse response = loginService.LoginUser(request);

            Assert.IsFalse(response.ValidUser, MSG_USER_NOT_FOUND);
        }

        [TestMethod]
        public void LoginUser_UserNotFound_ShouldReturnNegativeOneUserId()
        {
            var request = new LoginRequest { User = "nonexistent@test.com", Password = SEEDED_VALID_PASSWORD };
            LoginResponse response = loginService.LoginUser(request);

            Assert.AreEqual(-1, response.UserId, MSG_USER_ID_INVALID_RESPONSE);
        }

        [TestMethod]
        public void LoginUser_InvalidPassword_ShouldThrowInvalidPasswordFault()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = "WrongPassword123!" };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => loginService.LoginUser(request), MSG_INVALID_PASSWORD);
        }

        [TestMethod]
        public void LoginUser_InvalidPassword_ShouldHaveCorrectFaultCode()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = "WrongPassword123!" };
            string actualCode = string.Empty;

            try
            {
                loginService.LoginUser(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }

            Assert.AreEqual("LOGIN_INVALID_PASSWORD", actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void LoginUser_ValidCredentials_ShouldReturnValidUserTrue()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = SEEDED_VALID_PASSWORD };
            LoginResponse response = loginService.LoginUser(request);

            Assert.IsTrue(response.ValidUser, MSG_SUCCESSFUL_LOGIN);
        }

        [TestMethod]
        public void LoginUser_ValidCredentials_ShouldReturnCorrectUserId()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = SEEDED_VALID_PASSWORD };
            LoginResponse response = loginService.LoginUser(request);

            Assert.AreEqual(SEEDED_VALID_USER_ID, response.UserId, MSG_USER_ID_MATCH);
        }

        [TestMethod]
        public void LoginUser_ValidCredentials_ShouldReturnCorrectEmail()
        {
            var request = new LoginRequest { User = SEEDED_VALID_EMAIL, Password = SEEDED_VALID_PASSWORD };
            LoginResponse response = loginService.LoginUser(request);

            Assert.AreEqual(SEEDED_VALID_EMAIL, response.Email, "The returned email should match the input email.");
        }
    }
}