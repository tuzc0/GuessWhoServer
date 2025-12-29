using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Dtos;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Sessions;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters.InternalDtos;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class LoginCoordinator : ManagerBase, ILoginCoordinator
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(LoginCoordinator));

        private const string LOG_CTX_LOGIN_INIT = "LoginCoordinator.LoginAndInitializeSession";
        private const string LOG_CTX_LOGOUT = "LoginCoordinator.Logout";

        private const int MIN_VALID_ID = 1;

        private const UserSessionLoginStatus DEFAULT_FAILED_STATUS =
            UserSessionLoginStatus.InvalidCredentials;

        private readonly ILoginManager loginManager;
        private readonly IGameSessionManager sessionManager;
        private readonly IUserAccountRepository accountRepository;

        public LoginCoordinator(
            ILoginManager loginManager,
            IGameSessionManager sessionManager,
            IUserAccountRepository accountRepository)
        {
            this.loginManager = loginManager ??
                throw new ArgumentNullException(nameof(loginManager));
            this.sessionManager = sessionManager ??
                throw new ArgumentNullException(nameof(sessionManager));
            this.accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
        }

        public SessionLoginResult LoginAndInitializeSession(LoginArgs args)
        {
            return ExecuteService(
                LOG_CTX_LOGIN_INIT,
                () =>
                {
                    EnsureArgsNotNull(args);

                    SessionLoginResult loginResult = loginManager.Login(args);

                    if (loginResult == null || !loginResult.IsSuccess)
                    {
                        return loginResult ?? SessionLoginResult.CreateFailed(DEFAULT_FAILED_STATUS);
                    }

                    long userId = loginResult.Profile.UserId;

                    bool sessionsTerminated = sessionManager.TerminateActiveSessions(userId);

                    if (!sessionsTerminated)
                    {
                        Logger.WarnFormat("{0}: could not terminate active sessions for userId '{1}'.",
                            LOG_CTX_LOGIN_INIT,
                            userId);
                    }

                    bool markedActive = accountRepository.MarkUserProfileActive(userId);

                    if (!markedActive)
                    {
                        Logger.ErrorFormat("{0}: could not mark profile active for userId '{1}'.",
                            LOG_CTX_LOGIN_INIT,
                            userId);

                        throw FaultsFactory.Create(
                            LoginFaultKeys.CODE_UNEXPECTED_ERROR,
                            LoginFaultKeys.MSG_UNEXPECTED_ERROR,
                            LoginFaultKeys.FALLBACK_UNEXPECTED_ERROR);
                    }

                    return loginResult;
                });
        }

        public bool Logout(long userProfileId)
        {
            return ExecuteService(
                LOG_CTX_LOGOUT,
                () =>
                {
                    if (userProfileId < MIN_VALID_ID)
                    {
                        return false;
                    }

                    bool sessionsTerminated = sessionManager.TerminateActiveSessions(userProfileId);
                    bool markedInactive = accountRepository.MarkUserProfileInactive(userProfileId);

                    if (sessionsTerminated && markedInactive)
                    {
                        return true;
                    }

                    Logger.WarnFormat("{0}: logout failed for userProfileId '{1}'. sessionsTerminated='{2}', markedInactive='{3}'.",
                        LOG_CTX_LOGOUT,
                        userProfileId,
                        sessionsTerminated,
                        markedInactive);

                    throw FaultsFactory.Create(
                        LoginFaultKeys.CODE_LOGOUT_FAILED,
                        LoginFaultKeys.MSG_LOGOUT_FAILED,
                        LoginFaultKeys.FALLBACK_LOGOUT_FAILED);
                });
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }

        private static void EnsureArgsNotNull(LoginArgs args)
        {
            if (args != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                LoginFaultKeys.CODE_REQUEST_NULL,
                LoginFaultKeys.MSG_REQUEST_NULL,
                LoginFaultKeys.FALLBACK_REQUEST_NULL);
        }
    }
}
