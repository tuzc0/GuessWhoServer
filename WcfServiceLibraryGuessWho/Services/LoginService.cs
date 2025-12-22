using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.DataAccess.Match;
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
    public sealed class LoginService : ILoginService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginService));

        private const string FAULT_CODE_REQUEST_NULL = "REQUEST_LOGIN_NULL";
        private const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        private const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";
        private const string FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED = "LOGIN_CREDENTIALS_REQUIRED";
        private const string FAULT_CODE_LOGIN_INVALID_PASSWORD = "LOGIN_INVALID_PASSWORD";
        private const string FAULT_CODE_LOGIN_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";
        private const string FAULT_CODE_LOGIN_UPDATE_LAST_LOGIN_FAILED = "LOGIN_UPDATE_LAST_LOGIN_FAILED";

        private const string FAULT_CODE_LOGIN_ACCOUNT_LOCKED = "LOGIN_ACCOUNT_LOCKED";
        private const string FAULT_CODE_LOGIN_PROFILE_ALREADY_ACTIVE = "LOGIN_PROFILE_ALREADY_ACTIVE";
        private const string FAULT_CODE_LOGIN_ACCOUNT_NOT_FOUND = "LOGIN_ACCOUNT_NOT_FOUND";
        private const string FAULT_CODE_LOGIN_PROFILE_NOT_FOUND = "LOGIN_PROFILE_NOT_FOUND";

        private const string FAULT_CODE_REQUEST_LOGOUT_NULL = "REQUEST_LOGOUT_NULL";
        private const string FAULT_CODE_LOGOUT_FAILED = "LOGOUT_FAILED";
        private const string FAULT_CODE_LOGOUT_UNEXPECTED_ERROR = "LOGOUT_UNEXPECTED_ERROR";

        private const string FAULT_MESSAGE_REQUEST_NULL = 
            "Log data is missing. Please try again.";
        private const string FAULT_MESSAGE_LOGIN_CREDENTIALS_REQUIRED = 
            "Email and password are required to sign in.";
        private const string FAULT_MESSAGE_LOGIN_INVALID_PASSWORD = 
            "The password you entered is incorrect. Please try again.";
        private const string FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT = 
            "The server took too long to respond while processing the login. Please try again.";
        private const string FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE = 
            "The server could not connect to the database. Please try again later.";
        private const string FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR = 
            "An unexpected error occurred while processing the login. Please try again later.";
        private const string FAULT_MESSAGE_LOGIN_UPDATE_LAST_LOGIN_FAILED =
            "We could not complete the sign in because your session could not be updated. Please try again.";
        private const string FAULT_MESSAGE_LOGIN_ACCOUNT_LOCKED =
            "Your account is locked. Please try again later or contact support.";
        private const string FAULT_MESSAGE_LOGIN_PROFILE_ALREADY_ACTIVE =
            "Your account is already signed in on another device.";
        private const string FAULT_MESSAGE_LOGIN_ACCOUNT_NOT_FOUND =
            "The account could not be found or is no longer available.";
        private const string FAULT_MESSAGE_LOGIN_PROFILE_NOT_FOUND =
            "The account does not have a profile associated with it. Please contact support.";

        private const string FAULT_MESSAGE_REQUEST_LOGOUT_NULL =
            "Logout data is missing. Please try again.";
        private const string FAULT_MESSAGE_LOGOUT_FAILED =
            "The session could not be closed. Please try again.";
        private const string FAULT_MESSAGE_LOGOUT_UNEXPECTED_ERROR =
            "An unexpected error occurred while processing the logout. Please try again later.";

        private readonly UserAccountData userAccountData;
        private readonly MatchData matchData = new MatchData();

        public LoginService(UserAccountData userAccountData)
        {
            this.userAccountData = userAccountData ?? 
                throw new ArgumentNullException(nameof(userAccountData));

        }

        public LoginResponse LoginUser(LoginRequest request)
        {
            if (request == null)
            {
                Logger.Warn("LoginUser called with null request.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                string email = NormalizeEmail(request);
                string password = request.Password ?? string.Empty;

                var accountSearchParameters = new AccountSearchParameters
                {
                    Email = email
                };

                var result = userAccountData.GetAccountWithProfileForLogin(accountSearchParameters);

                switch (result.Status)
                {
                    case AccountProfileStatus.Success:

                        EnsurePasswordValidOrFault(password, result.Account);
                        EnsureLastLoginUpdatedOrFault(accountSearchParameters);

                        LeaveMatchesForUser(result.Profile.UserId);

                        return new LoginResponse
                        {
                            UserId = result.Profile.UserId,
                            DisplayName = result.Profile.DisplayName,
                            Email = result.Account.Email,
                            ValidUser = true
                        };

                    case AccountProfileStatus.Locked:
                        
                        Logger.WarnFormat("Login failed for email '{0}': account is locked.",
                            email);

                        throw Faults.Create(
                            FAULT_CODE_LOGIN_ACCOUNT_LOCKED,
                            FAULT_MESSAGE_LOGIN_ACCOUNT_LOCKED);

                    case AccountProfileStatus.ProfileAlreadyActive:
                        
                        Logger.WarnFormat("Login failed for email '{0}': profile is already active.",
                            email);

                        throw Faults.Create(
                            FAULT_CODE_LOGIN_PROFILE_ALREADY_ACTIVE,
                            FAULT_MESSAGE_LOGIN_PROFILE_ALREADY_ACTIVE);

                    case AccountProfileStatus.NotFoundOrDeleted:

                        Logger.WarnFormat("Login failed for email '{0}': account not found or deleted.",
                            email);

                        throw Faults.Create(
                            FAULT_CODE_LOGIN_ACCOUNT_NOT_FOUND,
                            FAULT_MESSAGE_LOGIN_ACCOUNT_NOT_FOUND);

                    case AccountProfileStatus.ProfileNotFound:
                        
                        Logger.WarnFormat("Login failed for email '{0}': profile not found.", email);

                        throw Faults.Create(
                            FAULT_CODE_LOGIN_PROFILE_NOT_FOUND,
                            FAULT_MESSAGE_LOGIN_PROFILE_NOT_FOUND);

                    default:

                        Logger.ErrorFormat("Login failed for email '{0}': unexpected account/profile status '{1}'.",
                            email, result.Status);

                        throw Faults.Create(
                            FAULT_CODE_LOGIN_UNEXPECTED_ERROR,
                            FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR);
                }
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                return HandleDatabaseExceptionForLogin(ex);
            }
            catch (Exception ex)
            {
                return HandleInfrastructureExceptionForLogin(ex);
            }
        }

        public BasicResponse LogoutUser(LogoutRequest request)
        {
            if (request == null)
            {
                Logger.Warn("LogoutUser called with null request.");

                throw Faults.Create(
                    FAULT_CODE_REQUEST_LOGOUT_NULL,
                    FAULT_MESSAGE_REQUEST_LOGOUT_NULL);
            }

            try
            {
                bool result = userAccountData.MarkUserProfileInactive(request.UserProfileId);

                if (!result)
                {
                    Logger.WarnFormat(
                        "LogoutUser failed: profile not found or could not be updated. UserProfileId={0}",
                        request.UserProfileId);

                    throw Faults.Create(
                        FAULT_CODE_LOGOUT_FAILED,
                        FAULT_MESSAGE_LOGOUT_FAILED);
                }

                return new BasicResponse
                {
                    Success = true
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                return HandleDatabaseExceptionForLogout(ex);
            }
            catch (Exception ex)
            {
                return HandleInfrastructureExceptionForLogout(ex);
            }
        }

        private string NormalizeEmail(LoginRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            string email = (request.Email ?? string.Empty).Trim();
            string password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw Faults.Create(
                    FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED,
                    FAULT_MESSAGE_LOGIN_CREDENTIALS_REQUIRED);
            }

            return email;
        }

        private void EnsurePasswordValidOrFault(string password, AccountDto accountDto)
        {
            bool isPasswordValid = PasswordHasher.Verify(password, accountDto.PasswordHash);

            if (!isPasswordValid)
            {
                throw Faults.Create(
                    FAULT_CODE_LOGIN_INVALID_PASSWORD,
                    FAULT_MESSAGE_LOGIN_INVALID_PASSWORD);
            }
        }

        private void EnsureLastLoginUpdatedOrFault(AccountSearchParameters accountSearchParameters)
        {
            bool isLastLoginUpdated = userAccountData.UpdateLastLoginUtc(accountSearchParameters);

            if (!isLastLoginUpdated)
            {

                throw Faults.Create(
                    FAULT_CODE_LOGIN_UPDATE_LAST_LOGIN_FAILED,
                    FAULT_MESSAGE_LOGIN_UPDATE_LAST_LOGIN_FAILED);
            }
        }

        private void LeaveMatchesForUser(long userId)
        {
            bool cleaned = matchData.ForceLeaveAllMatchesForUser(userId);

            Logger.InfoFormat("ForceLeaveAllMatchesForUser executed. UserId={0}, cleaned={1}",
                userId, cleaned);
        }

        private const string LOG_DB_TIMEOUT = "Database command timeout during login.";
        private const string LOG_DB_CONNECTION_FAILURE = "Database connection failure during login.";
        private const string LOG_DB_UNEXPECTED = "Unexpected DB error during login.";

        private BasicResponse HandleDatabaseExceptionForLogout(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_FAILURE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED, ex);
            throw Faults.Create(
                FAULT_CODE_LOGOUT_UNEXPECTED_ERROR,
                FAULT_MESSAGE_LOGOUT_UNEXPECTED_ERROR,
                ex);
        }

        private LoginResponse HandleDatabaseExceptionForLogin(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_FAILURE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED, ex);
            throw Faults.Create(
                FAULT_CODE_LOGIN_UNEXPECTED_ERROR,
                FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR,
                ex);
        }

        private LoginResponse HandleInfrastructureExceptionForLogin(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_FAILURE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal("Unexpected error during login.", ex);
            throw Faults.Create(
                FAULT_CODE_LOGIN_UNEXPECTED_ERROR,
                FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR,
                ex);
        }

        private BasicResponse HandleInfrastructureExceptionForLogout(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                            FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                            ex);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_FAILURE, ex);
                        throw Faults.Create(
                            FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                            FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                            ex);
                }
            }

            Logger.Fatal("Unexpected error during logout.", ex);
            throw Faults.Create(
                FAULT_CODE_LOGOUT_UNEXPECTED_ERROR,
                FAULT_MESSAGE_LOGOUT_UNEXPECTED_ERROR,
                ex);
        }
    
    }
}
