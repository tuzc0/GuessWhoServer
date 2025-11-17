using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    public class UpdateProfileService : IUpdateProfileService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UpdateProfileService));
        private readonly UserAccountData userAccountData = new UserAccountData();

        private const string FAULT_CODE_REQUEST_NULL = "PROFILE_REQUEST_NULL";
        private const string FAULT_CODE_USER_ID_INVALID = "PROFILE_USER_ID_INVALID";
        private const string FAULT_CODE_PROFILE_NOT_FOUND = "PROFILE_NOT_FOUND";
        private const string FAULT_CODE_NO_CHANGES_PROVIDED = "PROFILE_NO_CHANGES_PROVIDED";
        private const string FAULT_CODE_CURRENT_PASSWORD_REQUIRED = "PROFILE_CURRENT_PASSWORD_REQUIRED";
        private const string FAULT_CODE_CURRENT_PASSWORD_INCORRECT = "PROFILE_CURRENT_PASSWORD_INCORRECT";
        private const string FAULT_CODE_UNEXPECTED_GET_PROFILE_ERROR = "PROFILE_GET_UNEXPECTED_ERROR";
        private const string FAULT_CODE_UNEXPECTED_ERROR = "PROFILE_UPDATE_UNEXPECTED_ERROR";
        private const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        private const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";

        private const string FAULT_MESSAGE_REQUEST_NULL =
            "Profile request data is missing. Please try again.";
        private const string FAULT_MESSAGE_USER_ID_INVALID =
            "The profile identifier is not valid.";
        private const string FAULT_MESSAGE_PROFILE_NOT_FOUND =
            "We could not find a profile for the specified user.";
        private const string FAULT_MESSAGE_NO_CHANGES_PROVIDED =
            "No profile changes were provided.";
        private const string FAULT_MESSAGE_CURRENT_PASSWORD_REQUIRED =
            "The current password is required to change your password.";
        private const string FAULT_MESSAGE_CURRENT_PASSWORD_INCORRECT =
            "The current password you entered is incorrect.";
        private const string FAULT_MESSAGE_UNEXPECTED_GET_PROFILE_ERROR =
            "An unexpected error occurred while loading your profile. Please try again later.";
        private const string FAULT_MESSAGE_UNEXPECTED_UPDATE_PROFILE_ERROR =
            "An unexpected error occurred while updating your profile. Please try again later.";
        private const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT =
            "The server took too long to respond. Please try again.";
        private const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE =
            "The server could not connect to the database. Please try again later.";

        private const long INVALID_USER_ID = 0;
        private const string DEFAULT_AVATAR_URL = "";

        public GetProfileResponse GetProfile(GetProfileRequest request)
        {
            if (request == null)
            {
                Logger.Warn("GetProfile request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            long userId = request.UserId;

            if (userId <= INVALID_USER_ID)
            {
                Logger.Warn("GetProfile called with invalid UserId.");
                throw Faults.Create(
                    FAULT_CODE_USER_ID_INVALID,
                    FAULT_MESSAGE_USER_ID_INVALID);
            }

            try
            {
                var (account, profile) = LoadAccountAndProfileOrFault(userId);

                return new GetProfileResponse
                {
                    Username = profile.DisplayName,
                    Email = account.Email,
                    CreateAtUtc = account.CreatedAtUtc,
                    AvatarURL = DEFAULT_AVATAR_URL
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout while loading profile.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT, 
                    ex);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure while loading profile.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE, 
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in GetProfile.", ex);
                throw Faults.Create(
                    FAULT_CODE_UNEXPECTED_GET_PROFILE_ERROR,
                    FAULT_MESSAGE_UNEXPECTED_GET_PROFILE_ERROR, 
                    ex);
            }
        }

        public UpdateProfileResponse UpdateUserProfile(UpdateProfileRequest request)
        {
            ValidateUpdateRequestOrThrow(request);

            GetChangeIntents(request, out bool wantsNameChange, out bool wantsPasswordChange);

            try
            {
                var (account, profile) = LoadAccountAndProfileOrFault(request.UserId);

                byte[] newPasswordHash = null;

                if (wantsPasswordChange)
                {
                    newPasswordHash = ValidateAndComputeNewPasswordOrThrow(request, account.PasswordHash);
                }

                var updateAccountArgs = new UpdateAccountArgs
                {
                    UserId = request.UserId,
                    NewDisplayName = wantsNameChange
                    ? request.NewDisplayName.Trim()
                    : profile.DisplayName,
                    NewPassword = wantsPasswordChange
                    ? newPasswordHash
                    : account.PasswordHash,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                var (updatedAccount, updatedProfile) = userAccountData.UpdateDisplayNameAndPassword(updateAccountArgs);

                Logger.InfoFormat("UpdateUserProfile succeeded for userId '{0}'.", request.UserId);

                return new UpdateProfileResponse
                {
                    Updated = true,
                    Email = updatedAccount.Email,
                    Username = updatedProfile.DisplayName,
                    UpdatedAtUtc = updatedAccount.UpdatedAtUtc
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout while updating profile.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                    ex);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure while updating profile.", ex);
                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error in UpdateUserProfile.", ex);
                throw Faults.Create(
                    FAULT_CODE_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_UNEXPECTED_UPDATE_PROFILE_ERROR,
                    ex);
            }
        }

        private static void GetChangeIntents(UpdateProfileRequest request, out bool wantsNameChange,
            out bool wantsPasswordChange)
        {
            wantsNameChange = !string.IsNullOrWhiteSpace(request.NewDisplayName);
            wantsPasswordChange = !string.IsNullOrWhiteSpace(request.NewPasswordPlain);
        }

        private void ValidateUpdateRequestOrThrow(UpdateProfileRequest request)
        {
            if (request == null)
            {
                Logger.Warn("UpdateUserProfile request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            if (request.UserId <= INVALID_USER_ID)
            {
                Logger.Warn("UpdateUserProfile called with invalid UserId.");
                throw Faults.Create(
                    FAULT_CODE_USER_ID_INVALID,
                    FAULT_MESSAGE_USER_ID_INVALID);
            }

            bool wantsNameChange = !string.IsNullOrWhiteSpace(request.NewDisplayName);
            bool wantsPasswordChange = !string.IsNullOrWhiteSpace(request.NewPasswordPlain);

            if (!wantsNameChange && !wantsPasswordChange)
            {
                Logger.Warn("UpdateUserProfile called with no changes provided.");
                throw Faults.Create(
                    FAULT_CODE_NO_CHANGES_PROVIDED,
                    FAULT_MESSAGE_NO_CHANGES_PROVIDED);
            }
        }

        private (AccountDto Account, UserProfileDto Profile) LoadAccountAndProfileOrFault(long userId)
        {
            bool found = userAccountData.GetAccountWithProfileByIdAccount( userId, out AccountDto account,
                out UserProfileDto profile);

            if (!found)
            {
                Logger.WarnFormat("UpdateUserProfile failed: profile not found for userId '{0}'.", userId);
                throw Faults.Create(
                    FAULT_CODE_PROFILE_NOT_FOUND,
                    FAULT_MESSAGE_PROFILE_NOT_FOUND);
            }

            return (account, profile);
        }

        private byte[] ValidateAndComputeNewPasswordOrThrow(UpdateProfileRequest request,byte[] accountPassword)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPasswordPlain))
            {
                Logger.Warn("UpdateUserProfile password change requested without current password.");
                throw Faults.Create(
                    FAULT_CODE_CURRENT_PASSWORD_REQUIRED,
                    FAULT_MESSAGE_CURRENT_PASSWORD_REQUIRED);
            }

            if (!PasswordHasher.Verify(request.CurrentPasswordPlain, accountPassword))
            {
                Logger.Warn("UpdateUserProfile current password is incorrect.");
                throw Faults.Create(
                    FAULT_CODE_CURRENT_PASSWORD_INCORRECT,
                    FAULT_MESSAGE_CURRENT_PASSWORD_INCORRECT);
            }

            return PasswordHasher.HashPassword(request.NewPasswordPlain);
        }
    }
}
