using GuessWho.Services.WCF.Services;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;

namespace GuessWhoTests.GuessWhoServicesTests.Services
{
    [TestClass]
    public class UpdateProfileServiceTests
    {
        private UpdateProfileService updateProfileService;

        private const int SEEDED_USER_ID = 20003;
        private const string SEEDED_USERNAME = "TestUpdateProfile";
        private const string SEEDED_EMAIL = "testupdateprofile@gmail.com";
        private const string SEEDED_CURRENT_PASSWORD = "S3gur!dad2025";

        private const string MSG_REQUEST_NULL_THROWS = "Should throw PROFILE_REQUEST_NULL when request is null.";
        private const string MSG_REQUEST_NULL_CODE = "PROFILE_REQUEST_NULL";
        private const string MSG_USER_ID_INVALID_THROWS = "Should throw PROFILE_USER_ID_INVALID when UserId is <= 0.";
        private const string MSG_USER_ID_INVALID_CODE = "PROFILE_USER_ID_INVALID";
        private const string MSG_PROFILE_NOT_FOUND_THROWS = "Should throw PROFILE_NOT_FOUND when user does not exist.";
        private const string MSG_PROFILE_NOT_FOUND_CODE = "PROFILE_NOT_FOUND";
        private const string MSG_VALID_RESPONSE = "Should return a valid response object.";
        private const string MSG_USERNAME_MATCH = "Returned username should match seeded data.";
        private const string MSG_EMAIL_MATCH = "Returned email should match seeded data.";
        private const string MSG_NO_CHANGES_THROWS = "Should throw PROFILE_NO_CHANGES_PROVIDED when no update fields are set.";
        private const string MSG_NO_CHANGES_CODE = "PROFILE_NO_CHANGES_PROVIDED";
        private const string MSG_PASS_REQUIRED_THROWS = "Should throw PROFILE_CURRENT_PASSWORD_REQUIRED when updating password without current pass.";
        private const string MSG_PASS_REQUIRED_CODE = "PROFILE_CURRENT_PASSWORD_REQUIRED";
        private const string MSG_PASS_INCORRECT_THROWS = "Should throw PROFILE_CURRENT_PASSWORD_INCORRECT when current password does not match.";
        private const string MSG_PASS_INCORRECT_CODE = "PROFILE_CURRENT_PASSWORD_INCORRECT";
        private const string MSG_UPDATE_SUCCESS = "Should return Updated = true on successful update.";
        private const string MSG_UPDATE_NAME_MATCH = "Returned username should match the requested new name.";
        private const string MSG_DELETE_FAILED_THROWS = "Should throw PROFILE_DELETE_FAILED when user to delete does not exist.";
        private const string MSG_DELETE_FAILED_CODE = "PROFILE_DELETE_FAILED";
        private const string MSG_DELETE_SUCCESS = "Should return Success = true on successful deletion.";
        private const string MSG_FAULT_CODE_MATCH = "FaultCode should match expected value.";

        [TestInitialize]
        public void Setup()
        {
            DatabaseResetter.ResetDatabase();
            updateProfileService = new UpdateProfileService();
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            DatabaseResetter.ResetDatabase();
        }

        [TestMethod]
        public void GetProfile_NullRequest_ShouldThrowFaultException()
        {
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.GetProfile(null), MSG_REQUEST_NULL_THROWS);
        }

        [TestMethod]
        public void GetProfile_NullRequest_ShouldHaveCorrectFaultCode()
        {
            string actualCode = string.Empty;
            try
            {
                updateProfileService.GetProfile(null);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_REQUEST_NULL_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void GetProfile_InvalidUserId_ShouldThrowFaultException()
        {
            var request = new GetProfileRequest { UserId = 0 };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.GetProfile(request), MSG_USER_ID_INVALID_THROWS);
        }

        [TestMethod]
        public void GetProfile_InvalidUserId_ShouldHaveCorrectFaultCode()
        {
            var request = new GetProfileRequest { UserId = -1 };
            string actualCode = string.Empty;
            try
            {
                updateProfileService.GetProfile(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_USER_ID_INVALID_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void GetProfile_NonExistentUser_ShouldThrowFaultException()
        {
            var request = new GetProfileRequest { UserId = 999999 };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.GetProfile(request), MSG_PROFILE_NOT_FOUND_THROWS);
        }

        [TestMethod]
        public void GetProfile_NonExistentUser_ShouldHaveCorrectFaultCode()
        {
            var request = new GetProfileRequest { UserId = 999999 };
            string actualCode = string.Empty;
            try
            {
                updateProfileService.GetProfile(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_PROFILE_NOT_FOUND_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void GetProfile_ValidUser_ShouldReturnResponse()
        {
            var request = new GetProfileRequest { UserId = SEEDED_USER_ID };
            var response = updateProfileService.GetProfile(request);
            Assert.IsNotNull(response, MSG_VALID_RESPONSE);
        }

        [TestMethod]
        public void GetProfile_ValidUser_ShouldReturnCorrectUsername()
        {
            var request = new GetProfileRequest { UserId = SEEDED_USER_ID };
            var response = updateProfileService.GetProfile(request);
            Assert.AreEqual(SEEDED_USERNAME, response.Username, MSG_USERNAME_MATCH);
        }

        [TestMethod]
        public void GetProfile_ValidUser_ShouldReturnCorrectEmail()
        {
            var request = new GetProfileRequest { UserId = SEEDED_USER_ID };
            var response = updateProfileService.GetProfile(request);
            Assert.AreEqual(SEEDED_EMAIL, response.Email, MSG_EMAIL_MATCH);
        }

        [TestMethod]
        public void UpdateUserProfile_NullRequest_ShouldThrowFaultException()
        {
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.UpdateUserProfile(null), MSG_REQUEST_NULL_THROWS);
        }

        [TestMethod]
        public void UpdateUserProfile_NullRequest_ShouldHaveCorrectFaultCode()
        {
            string actualCode = string.Empty;
            try
            {
                updateProfileService.UpdateUserProfile(null);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_REQUEST_NULL_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void UpdateUserProfile_NoChangesProvided_ShouldThrowFaultException()
        {
            var request = new UpdateProfileRequest { UserId = SEEDED_USER_ID };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.UpdateUserProfile(request), MSG_NO_CHANGES_THROWS);
        }

        [TestMethod]
        public void UpdateUserProfile_NoChangesProvided_ShouldHaveCorrectFaultCode()
        {
            var request = new UpdateProfileRequest { UserId = SEEDED_USER_ID, NewDisplayName = "    " };
            string actualCode = string.Empty;
            try
            {
                updateProfileService.UpdateUserProfile(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_NO_CHANGES_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void UpdateUserProfile_PasswordChangeWithoutCurrent_ShouldThrowFaultException()
        {
            var request = new UpdateProfileRequest
            {
                UserId = SEEDED_USER_ID,
                NewPasswordPlain = "NewPass123!",
                CurrentPasswordPlain = ""
            };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.UpdateUserProfile(request), MSG_PASS_REQUIRED_THROWS);
        }

        [TestMethod]
        public void UpdateUserProfile_PasswordChangeWithoutCurrent_ShouldHaveCorrectFaultCode()
        {
            var request = new UpdateProfileRequest
            {
                UserId = SEEDED_USER_ID,
                NewPasswordPlain = "NewPass123!",
                CurrentPasswordPlain = null
            };
            string actualCode = string.Empty;
            try
            {
                updateProfileService.UpdateUserProfile(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_PASS_REQUIRED_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void UpdateUserProfile_IncorrectCurrentPassword_ShouldThrowFaultException()
        {
            var request = new UpdateProfileRequest
            {
                UserId = SEEDED_USER_ID,
                NewPasswordPlain = "NewPass123!",
                CurrentPasswordPlain = "WrongPass"
            };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.UpdateUserProfile(request), MSG_PASS_INCORRECT_THROWS);
        }

        [TestMethod]
        public void UpdateUserProfile_IncorrectCurrentPassword_ShouldHaveCorrectFaultCode()
        {
            var request = new UpdateProfileRequest
            {
                UserId = SEEDED_USER_ID,
                NewPasswordPlain = "NewPass123!",
                CurrentPasswordPlain = "WrongPass"
            };
            string actualCode = string.Empty;
            try
            {
                updateProfileService.UpdateUserProfile(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_PASS_INCORRECT_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void UpdateUserProfile_ValidNameChange_ShouldReturnUpdatedTrue()
        {
            var request = new UpdateProfileRequest
            {
                UserId = SEEDED_USER_ID,
                NewDisplayName = "UpdatedNameTest"
            };

            var response = updateProfileService.UpdateUserProfile(request);

            Assert.IsTrue(response.Updated, MSG_UPDATE_SUCCESS);
        }

        [TestMethod]
        public void UpdateUserProfile_ValidNameChange_ShouldReturnNewName()
        {
            var request = new UpdateProfileRequest
            {
                UserId = SEEDED_USER_ID,
                NewDisplayName = "UpdatedNameTestNew"
            };

            var response = updateProfileService.UpdateUserProfile(request);

            Assert.AreEqual("UpdatedNameTestNew", response.Username, MSG_UPDATE_NAME_MATCH);
        }

        [TestMethod]
        public void DeleteUserProfile_NullRequest_ShouldThrowFaultException()
        {
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.DeleteUserProfile(null), MSG_REQUEST_NULL_THROWS);
        }

        [TestMethod]
        public void DeleteUserProfile_NullRequest_ShouldHaveCorrectFaultCode()
        {
            string actualCode = string.Empty;
            try
            {
                updateProfileService.DeleteUserProfile(null);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_REQUEST_NULL_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void DeleteUserProfile_NonExistentUser_ShouldThrowFaultException()
        {
            var request = new DeleteProfileRequest { UserId = 999999 };
            Assert.ThrowsException<FaultException<ServiceFault>>(() => updateProfileService.DeleteUserProfile(request), MSG_DELETE_FAILED_THROWS);
        }

        [TestMethod]
        public void DeleteUserProfile_NonExistentUser_ShouldHaveCorrectFaultCode()
        {
            var request = new DeleteProfileRequest { UserId = 999999 };
            string actualCode = string.Empty;
            try
            {
                updateProfileService.DeleteUserProfile(request);
            }
            catch (FaultException<ServiceFault> ex)
            {
                actualCode = ex.Detail.Code;
            }
            Assert.AreEqual(MSG_DELETE_FAILED_CODE, actualCode, MSG_FAULT_CODE_MATCH);
        }

        [TestMethod]
        public void DeleteUserProfile_ValidUser_ShouldReturnSuccess()
        {
            var request = new DeleteProfileRequest { UserId = SEEDED_USER_ID };

            var response = updateProfileService.DeleteUserProfile(request);

            Assert.IsTrue(response.Success, MSG_DELETE_SUCCESS);
        }
    }
}