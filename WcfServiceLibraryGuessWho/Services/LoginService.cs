using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Request;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServerDomain.Domain.Models.Session;

namespace GuessWhoServices.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class LoginService : ServiceBase, ILoginService
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(LoginService));

        private const string LOG_CTX_LOGIN_USER = "LoginService.LoginUser";
        private const string LOG_CTX_LOGOUT_USER = "LoginService.LogoutUser";

        private const string EMPTY = "";

        private readonly ILoginCoordinator loginCoordinator;

        public LoginService(ILoginCoordinator loginCoordinator)
        {
            this.loginCoordinator = loginCoordinator ??
                throw new ArgumentNullException(nameof(loginCoordinator));
        }

        public LoginResponse LoginUser(LoginRequest request)
        {
            return ExecuteService(
                LOG_CTX_LOGIN_USER,
                () =>
                {
                    EnsureRequestNotNull(request);

                    LoginArgs loginArgs = BuildLoginArgs(request);

                    SessionLoginResult result = loginCoordinator.LoginAndInitializeSession(loginArgs);

                    if (result == null || !result.IsSuccess)
                    {
                        Logger.InfoFormat(
                            "{0}: login failed for email '{1}'.",
                            LOG_CTX_LOGIN_USER,
                            NormalizeEmail(request.Email));
                    }

                    return new LoginResponse
                    {
                        UserId = result.Profile.UserId,
                        DisplayName = result.Profile.DisplayName,
                        Email = result.Account.Email,
                        ValidUser = true
                    };
                });
        }

        public BasicResponse LogoutUser(LogoutRequest request)
        {
            return ExecuteService(
                LOG_CTX_LOGOUT_USER,
                () =>
                {
                    EnsureRequestNotNull(request);

                    bool success = loginCoordinator.Logout(request.UserProfileId);

                    return new BasicResponse
                    {
                        Success = success
                    };
                });
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(LoginFaultKeys.CODE_REQUEST_NULL);
        }

        private static LoginArgs BuildLoginArgs(LoginRequest request)
        {
            string normalizedEmail = NormalizeEmail(request.Email);
            string safePassword = request.Password ?? EMPTY;

            return new LoginArgs(normalizedEmail, safePassword);
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? EMPTY).Trim().ToLowerInvariant();
        }
    }
}
