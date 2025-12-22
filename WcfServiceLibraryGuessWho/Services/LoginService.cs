using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators;
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

        private readonly ILoginManager loginManager;
        private readonly ILoginFaultMapper loginFaultMapper;

        public LoginService(
            ILoginManager loginManager,
            ILoginFaultMapper loginFaultMapper)
        {
            this.loginManager = loginManager ??
                throw new ArgumentNullException(nameof(loginManager));
            this.loginFaultMapper = loginFaultMapper ??
                throw new ArgumentNullException(nameof(loginFaultMapper));
        }

        public LoginResponse LoginUser(LoginRequest request)
        {
            if (request == null)
            {
                Logger.Warn("LoginUser request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            LoginArgs loginArgs = BuildLoginArgs(request);

            try
            {
                LoginResult result = loginManager.Login(loginArgs);

                return new LoginResponse
                {
                    UserId = result.UserId,
                    DisplayName = result.DisplayName,
                    Email = result.Email,
                    ValidUser = true
                };
            }
            catch (ArgumentException ex)
            {
                Logger.Warn("LoginUser failed due to invalid input.", ex);

                // IMPORTANTE: Se usa la constante local, NO ex.Message
                throw Faults.Create(
                    FAULT_CODE_LOGIN_INVALID_INPUT,
                    FAULT_MESSAGE_LOGIN_INVALID_INPUT,
                    ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error("LoginUser failed due to invalid operation.", ex);

                // IMPORTANTE: Se usa la constante local para ocultar detalles del Manager
                throw Faults.Create(
                    FAULT_CODE_LOGIN_FAILED,
                    FAULT_MESSAGE_LOGIN_FAILED,
                    ex);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw loginFaultMapper.MapLoginDbException(ex);
            }
            catch (Exception ex)
            {
                throw loginFaultMapper.MapLoginException(ex);
            }
        }

        public BasicResponse LogoutUser(LogoutRequest request)
        {
            if (request == null)
            {
                Logger.Warn("LogoutUser request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                bool result = loginManager.Logout(request.UserProfileId);
                return new BasicResponse { Success = result };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw loginFaultMapper.MapLogoutDbException(ex);
            }
            catch (Exception ex)
            {
                throw loginFaultMapper.MapLogoutException(ex);
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