using System;
using System.ServiceModel;
using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false)]
    public class LoginService : ILoginService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginService));

        private const string FAULT_CODE_REQUEST_NULL = "REQUEST_NULL";
        private const string FAULT_CODE_DATABASE_COMMAND_TIMEOUT = "DATABASE_COMMAND_TIMEOUT";
        private const string FAULT_CODE_DATABASE_CONNECTION_FAILURE = "DATABASE_CONNECTION_FAILURE";

        private const string FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED = "LOGIN_CREDENTIALS_REQUIRED";
        private const string FAULT_CODE_LOGIN_INVALID_PASSWORD = "LOGIN_INVALID_PASSWORD";
        private const string FAULT_CODE_LOGIN_UNEXPECTED_ERROR = "LOGIN_UNEXPECTED_ERROR";

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

        private readonly UserAccountData userAccountData = new UserAccountData();

        public LoginResponse LoginUser(LoginRequest request)
        {
            if (request == null)
            {
                Logger.Warn("Login request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            string email = (request.User ?? string.Empty).Trim().ToLowerInvariant();
            string password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Logger.Warn("Login request with missing email or password.");
                throw Faults.Create(
                    FAULT_CODE_LOGIN_CREDENTIALS_REQUIRED,
                    FAULT_MESSAGE_LOGIN_CREDENTIALS_REQUIRED);
            }

            DateTime nowUtc = DateTime.UtcNow;

            try
            {
                Logger.Info("Login attempt for email.");

                var loginAccountArgs = new LoginAccountArgs
                {
                    Email = email,
                    LastLoginUtcDate = nowUtc
                };

                bool found = userAccountData.TryGetAccountWithProfileByEmail(
                    loginAccountArgs,
                    out AccountDto accountDto,
                    out UserProfileDto profileDto);

                if (!found)
                {
                    Logger.Warn("Login failed: account not found for email '{email}'.");

                    return new LoginResponse
                    {
                        UserId = -1,
                        DisplayName = "Invalid user",
                        Email = string.Empty,
                        ValidUser = false
                    };
                }

                if (!PasswordHasher.Verify(password, accountDto.PasswordHash))
                {
                    Logger.Warn("Login failed: invalid password for email '{email}'.");

                    throw Faults.Create(
                        FAULT_CODE_LOGIN_INVALID_PASSWORD,
                        FAULT_MESSAGE_LOGIN_INVALID_PASSWORD);
                }

                userAccountData.UpdateLastLoginUtc(loginAccountArgs);

                Logger.Info("Login successful for email '{email}'.");

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
    }
}
