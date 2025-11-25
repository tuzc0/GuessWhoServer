using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.DataAccess.Match;
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
    [ServiceBehavior(IncludeExceptionDetailInFaults = false)]
    public class LoginService : ILoginService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginService));

        private const long INVALID_USER_ID = -1;
        private const string INVALID_USER_DISPLAY_NAME = "Invalid user";

        private const string FAULT_CODE_REQUEST_NULL = "REQUEST_NULL";
        private const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        private const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";
        private const string FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED = "LOGIN_CREDENTIALS_REQUIRED";
        private const string FAULT_CODE_LOGIN_INVALID_PASSWORD = "LOGIN_INVALID_PASSWORD";
        private const string FAULT_CODE_LOGIN_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";
        private const string FAULT_CODE_LOGIN_UPDATE_LAST_LOGIN_FAILED = "LOGIN_UPDATE_LAST_LOGIN_FAILED";

        private const string FAULT_MESSAGE_REQUEST_NULL =
            "Login data is missing. Please try again.";
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

        private readonly UserAccountData userAccountData = new UserAccountData();
        private readonly MatchData matchData = new MatchData();

        public LoginResponse LoginUser(LoginRequest request)
        {
            try
            {
                string email = NormalizeEmail(request);
                string password = request.Password ?? string.Empty;

                var accountSearchParameters = new AccountSearchParameters
                {
                    Email = email
                };

                var (accountDto, profileDto) = userAccountData.TryGetAccountWithProfile(accountSearchParameters);

                if (!accountDto.IsValid || !profileDto.IsValid)
                {
                    Logger.WarnFormat("Login failed: account/profile not found for email '{0}'.", email);

                    return new LoginResponse
                    {
                        UserId = INVALID_USER_ID,
                        DisplayName = INVALID_USER_DISPLAY_NAME,
                        Email = string.Empty,
                        ValidUser = false
                    };
                }

                EnsurePasswordValidOrFault(password, accountDto, email);
                EnsureLastLoginUpdatedOrFault(accountSearchParameters, email);

                Logger.InfoFormat("Login successful for email '{0}'.", email);

                LeaveMatchesForUser(profileDto.UserId);

                return new LoginResponse
                {
                    UserId = profileDto.UserId,
                    DisplayName = profileDto.DisplayName,
                    Email = accountDto.Email,
                    ValidUser = true
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex) when (SqlExceptionInspector.IsCommandTimeout(ex))
            {
                Logger.Fatal("Database command timeout during login.", ex);

                throw Faults.Create(
                    FAULT_CODE_DATABASE_COMMAND_TIMEOUT,
                    FAULT_MESSAGE_DATABASE_COMMAND_TIMEOUT,
                    ex);
            }
            catch (Exception ex) when (SqlExceptionInspector.IsConnectionFailure(ex))
            {
                Logger.Fatal("Database connection failure during login.", ex);

                throw Faults.Create(
                    FAULT_CODE_DATABASE_CONNECTION_FAILURE,
                    FAULT_MESSAGE_DATABASE_CONNECTION_FAILURE,
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected error during login.", ex);

                throw Faults.Create(
                    FAULT_CODE_LOGIN_UNEXPECTED_ERROR,
                    FAULT_MESSAGE_LOGIN_UNEXPECTED_ERROR,
                    ex);
            }
        }

        private string NormalizeEmail(LoginRequest request)
        {
            if (request == null)
            {
                Logger.Warn("Login request is null.");

                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            string email = (request.User ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
            string password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Logger.Warn("Login request with missing email or password.");

                throw Faults.Create(
                    FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED,
                    FAULT_MESSAGE_LOGIN_CREDENTIALS_REQUIRED);
            }

            return email;
        }

        private void EnsurePasswordValidOrFault(string password, AccountDto accountDto, string email)
        {
            bool isPasswordValid = PasswordHasher.Verify(password, accountDto.PasswordHash);

            if (!isPasswordValid)
            {
                Logger.WarnFormat(
                    "Login failed: invalid password for email '{0}'.",
                    email);

                throw Faults.Create(
                    FAULT_CODE_LOGIN_INVALID_PASSWORD,
                    FAULT_MESSAGE_LOGIN_INVALID_PASSWORD);
            }
        }

        private void EnsureLastLoginUpdatedOrFault(AccountSearchParameters accountSearchParameters, string email)
        {
            bool isLastLoginUpdated = userAccountData.UpdateLastLoginUtc(accountSearchParameters);

            if (!isLastLoginUpdated)
            {
                Logger.WarnFormat(
                    "Login failed: could not update last login time for email '{0}'.",
                    email);

                throw Faults.Create(
                    FAULT_CODE_LOGIN_UPDATE_LAST_LOGIN_FAILED,
                    FAULT_MESSAGE_LOGIN_UPDATE_LAST_LOGIN_FAILED);
            }
        }

        private void LeaveMatchesForUser(long userId)
        {
            bool cleaned = matchData.ForceLeaveAllMatchesForUser(userId);
            Logger.InfoFormat("ForceLeaveAllMatchesForUser executed. UserId={0}, cleaned={1}", userId, cleaned);
        }
    }
}
