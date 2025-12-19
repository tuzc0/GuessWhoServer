using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.Single)]
    public class UpdateProfileService : IUpdateProfileService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UpdateProfileService));
        private readonly UserAccountData userAccountData;

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
        private const string FAULT_CODE_PROFILE_DELETE_FAILED = "PROFILE_DELETE_FAILED";

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
        private const string FAULT_MESSAGE_PROFILE_DELETE_FAILED =
            "We could not delete your profile. Please try again later.";

        private const string LOG_DB_TIMEOUT_GET_PROFILE = "Database command timeout while loading profile.";
        private const string LOG_DB_CONNECTION_GET_PROFILE = "Database connection failure while loading profile.";
        private const string LOG_DB_UNEXPECTED_GET_PROFILE = "Unexpected DB error while loading profile.";
        private const string LOG_UNEXPECTED_GET_PROFILE = "Unexpected error in GetProfile.";

        private const string LOG_DB_TIMEOUT_UPDATE_PROFILE = "Database command timeout while updating profile.";
        private const string LOG_DB_CONNECTION_UPDATE_PROFILE = "Database connection failure while updating profile.";
        private const string LOG_DB_UNEXPECTED_UPDATE_PROFILE = "Unexpected DB error while updating profile.";
        private const string LOG_UNEXPECTED_UPDATE_PROFILE = "Unexpected error in UpdateUserProfile.";

        private const string LOG_DB_TIMEOUT_DELETE_PROFILE = "Database command timeout while deleting profile.";
        private const string LOG_DB_CONNECTION_DELETE_PROFILE = "Database connection failure while deleting profile.";
        private const string LOG_DB_UNEXPECTED_DELETE_PROFILE = "Unexpected DB error while deleting profile.";
        private const string LOG_UNEXPECTED_DELETE_PROFILE = "Unexpected error in DeleteUserProfile.";

        private const long INVALID_USER_ID = 0;

        public UpdateProfileService(UserAccountData userAccountData)
        {
            this.userAccountData = userAccountData ?? 
                throw new ArgumentNullException(nameof(userAccountData));
        }

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
                    AvatarId = profile.AvatarId
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex) 
            { 
                return HandleDatabaseExceptionForGetProfile(ex); 
            }
            catch (Exception ex) 
            { 
                return HandleInfrastructureExceptionForGetProfile(ex); 
            }
        }

        public UpdateProfileResponse UpdateUserProfile(UpdateProfileRequest request)
        {
            ValidateUpdateRequestOrThrow(request);

            GetChangeIntents(request, out bool wantsNameChange, out bool wantsPasswordChange, out bool wantsAvatarChange);

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
                    UpdatedAtUtc = DateTime.UtcNow,
                    NewAvatarId = wantsAvatarChange 
                    ? request.NewAvatarId.Trim() : profile.AvatarId
                };

                var (updatedAccount, updatedProfile) = userAccountData.UpdateDisplayNameAndPassword(updateAccountArgs);

                return new UpdateProfileResponse
                {
                    Updated = true,
                    Email = updatedAccount.Email,
                    Username = updatedProfile.DisplayName,
                    UpdatedAtUtc = updatedAccount.UpdatedAtUtc,
                    AvatarId = updatedProfile.AvatarId
                };
            }
            catch (FaultException<ServiceFault>)
            { 
                throw; 
            }
            catch (DbUpdateException ex) 
            { 
                return HandleDatabaseExceptionForUpdateProfile(ex); 
            }
            catch (Exception ex) 
            { 
                return HandleInfrastructureExceptionForUpdateProfile(ex); 
            }
        }

        public BasicResponse DeleteUserProfile(DeleteProfileRequest request)
        {
            if (request == null)
            {
                Logger.Warn("DeleteUserProfile request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            if (request.UserId <= INVALID_USER_ID)
            {
                Logger.Warn("DeleteUserProfile called with invalid UserId.");
                throw Faults.Create(
                    FAULT_CODE_USER_ID_INVALID,
                    FAULT_MESSAGE_USER_ID_INVALID);
            }

            try
            {
                bool success = userAccountData.DeleteAccount(request.UserId);

                if (!success)
                {
                    Logger.WarnFormat("DeleteUserProfile failed: profile not found for userId '{0}'.", request.UserId);
                    throw Faults.Create(
                        FAULT_CODE_PROFILE_DELETE_FAILED,
                        FAULT_MESSAGE_PROFILE_DELETE_FAILED);
                }

                return new BasicResponse
                {
                    Success = success
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex) 
            { 
                return HandleDatabaseExceptionForDeleteProfile(ex);
            }
            catch (Exception ex)
            { 
                return HandleInfrastructureExceptionForDeleteProfile(ex); 
            }
        }

        private static void GetChangeIntents(UpdateProfileRequest request, out bool wantsNameChange,
            out bool wantsPasswordChange, out bool wantsAvatarChange)
        {
            wantsNameChange = !string.IsNullOrWhiteSpace(request.NewDisplayName);
            wantsPasswordChange = !string.IsNullOrWhiteSpace(request.NewPasswordPlain);
            wantsAvatarChange = !string.IsNullOrWhiteSpace(request.NewAvatarId);
        }

        private static void ValidateUpdateRequestOrThrow(UpdateProfileRequest request)
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

            GetChangeIntents(request, out bool wantsNameChange, out bool wantsPasswordChange, out bool wantsAvatarChange);

            if (!wantsNameChange && !wantsPasswordChange && !wantsAvatarChange)
            {
                Logger.Warn("UpdateUserProfile called with no changes provided.");
                throw Faults.Create(
                    FAULT_CODE_NO_CHANGES_PROVIDED,
                    FAULT_MESSAGE_NO_CHANGES_PROVIDED);
            }
        }

        private (AccountDto Account, UserProfileDto Profile) LoadAccountAndProfileOrFault(long userId)
        {
            var args = new AccountSearchParameters
            {
                UserId = userId,
                Email = string.Empty
            };

            var result = userAccountData.TryGetAccountWithProfileForUpdate(args);
            var account = result.account;
            var profile = result.profile;

            if (!account.IsValid || !profile.IsValid)
            {
                Logger.WarnFormat(
                    "LoadAccountAndProfileOrFault failed: account/profile not found for email '{0}' and userId '{1}'.",
                    args.Email ?? string.Empty,
                    args.UserId);

                throw Faults.Create(
                    FAULT_CODE_PROFILE_NOT_FOUND,
                    FAULT_MESSAGE_PROFILE_NOT_FOUND);
            }

            return (account, profile);
        }


        private static byte[] ValidateAndComputeNewPasswordOrThrow(UpdateProfileRequest request,byte[] accountPassword)
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

        private GetProfileResponse HandleDatabaseExceptionForGetProfile(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_GET_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_GET_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_GET_PROFILE, ex);
            throw Faults.Create(
                FAULT_CODE_UNEXPECTED_GET_PROFILE_ERROR,
                FAULT_MESSAGE_UNEXPECTED_GET_PROFILE_ERROR,
                ex);
        }

        private GetProfileResponse HandleInfrastructureExceptionForGetProfile(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_GET_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_GET_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Error(LOG_UNEXPECTED_GET_PROFILE, ex);
            throw Faults.Create(
                FAULT_CODE_UNEXPECTED_GET_PROFILE_ERROR,
                FAULT_MESSAGE_UNEXPECTED_GET_PROFILE_ERROR,
                ex);
        }

        private UpdateProfileResponse HandleDatabaseExceptionForUpdateProfile(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_UPDATE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_UPDATE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_UPDATE_PROFILE, ex);
            throw Faults.Create(
                FAULT_CODE_UNEXPECTED_ERROR,
                FAULT_MESSAGE_UNEXPECTED_UPDATE_PROFILE_ERROR,
                ex);
        }

        private UpdateProfileResponse HandleInfrastructureExceptionForUpdateProfile(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_UPDATE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_UPDATE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Error(LOG_UNEXPECTED_UPDATE_PROFILE, ex);
            throw Faults.Create(
                FAULT_CODE_UNEXPECTED_ERROR,
                FAULT_MESSAGE_UNEXPECTED_UPDATE_PROFILE_ERROR,
                ex);
        }

        private BasicResponse HandleDatabaseExceptionForDeleteProfile(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_DELETE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_DELETE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_DELETE_PROFILE, ex);
            throw Faults.Create(
                FAULT_CODE_UNEXPECTED_ERROR,
                FAULT_MESSAGE_UNEXPECTED_UPDATE_PROFILE_ERROR,
                ex);
        }

        private BasicResponse HandleInfrastructureExceptionForDeleteProfile(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_DELETE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_DELETE_PROFILE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Error(LOG_UNEXPECTED_DELETE_PROFILE, ex);
            throw Faults.Create(
                FAULT_CODE_UNEXPECTED_ERROR,
                FAULT_MESSAGE_UNEXPECTED_UPDATE_PROFILE_ERROR,
                ex);
        }
    }
}
