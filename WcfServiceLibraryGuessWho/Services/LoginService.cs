using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using GuessWhoContracts.Enums;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class LoginService : ILoginService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginService));

        private const string FAULT_CODE_REQUEST_NULL = "LOGIN_REQUEST_NULL";
        private const string FAULT_MESSAGE_REQUEST_NULL = "The request cannot be null.";

        private const string FAULT_CODE_LOGIN_INVALID_INPUT = "LOGIN_INVALID_INPUT";
        private const string FAULT_MESSAGE_LOGIN_INVALID_INPUT =
            "Some login fields are invalid. Please check your information and try again.";

        private const string FAULT_CODE_LOGIN_FAILED = "LOGIN_FAILED";
        private const string FAULT_MESSAGE_LOGIN_FAILED =
            "Your login could not be processed. Please check your credentials or try again later.";

        private readonly ILoginCoordinator _loginCoordinator;
        private readonly ILoginFaultMapper _loginFaultMapper;

        public LoginService(
            ILoginCoordinator loginCoordinator,
            ILoginFaultMapper loginFaultMapper)
        {
            _loginCoordinator = loginCoordinator ??
                throw new ArgumentNullException(nameof(loginCoordinator));
            _loginFaultMapper = loginFaultMapper ??
                throw new ArgumentNullException(nameof(loginFaultMapper));
        }

        public LoginResponse LoginUser(LoginRequest request)
        {
            if (request == null)
            {
                Logger.Warn("LoginUser request is null.");
                throw Faults.Create(FAULT_CODE_REQUEST_NULL, FAULT_MESSAGE_REQUEST_NULL);
            }

            LoginArgs loginArgs = BuildLoginArgs(request);

            try
            {
                var result = _loginCoordinator.LoginAndInitializeSession(loginArgs);

                if (!result.IsSuccess)
                {
                    Logger.Info($"Login failed for user {request.Email}. Status: {result.Status}");

                    return new LoginResponse
                    {
                        ValidUser = false
                    };
                }

                return new LoginResponse
                {
                    UserId = result.Profile.UserId,
                    DisplayName = result.Profile.DisplayName,
                    Email = result.Account.Email,
                    ValidUser = true
                };
            }
            catch (ArgumentException ex)
            {
                Logger.Warn("LoginUser failed due to invalid input.", ex);
                throw Faults.Create(FAULT_CODE_LOGIN_INVALID_INPUT, FAULT_MESSAGE_LOGIN_INVALID_INPUT, ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error("LoginUser failed due to invalid operation.", ex);
                throw Faults.Create(FAULT_CODE_LOGIN_FAILED, FAULT_MESSAGE_LOGIN_FAILED, ex);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw _loginFaultMapper.MapLoginDbException(ex);
            }
            catch (Exception ex)
            {
                throw _loginFaultMapper.MapLoginException(ex);
            }
        }

        public BasicResponse LogoutUser(LogoutRequest request)
        {
            if (request == null)
            {
                Logger.Warn("LogoutUser request is null.");
                throw Faults.Create(FAULT_CODE_REQUEST_NULL, FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                bool result = _loginCoordinator.Logout(request.UserProfileId);
                return new BasicResponse { Success = result };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw _loginFaultMapper.MapLogoutDbException(ex);
            }
            catch (Exception ex)
            {
                throw _loginFaultMapper.MapLogoutException(ex);
            }
        }

        private static LoginArgs BuildLoginArgs(LoginRequest request)
        {
            string normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string safePassword = request.Password ?? string.Empty;

            return new LoginArgs(normalizedEmail, safePassword);
        }
    }
}