using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Dtos;
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
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(LoginCoordinator));

        private const string LOG_CTX_LOGIN_INIT = "LoginCoordinator.LoginAndInitializeSession";
        private const string LOG_CTX_LOGOUT = "LoginCoordinator.Logout";

        private const int MIN_VALID_ID = 1;

        private const UserSessionLoginStatus DEFAULT_FAILED_STATUS = UserSessionLoginStatus.InvalidCredentials;

        private readonly ILoginManager loginManager;
        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public LoginCoordinator(
            ILoginManager loginManager,
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.loginManager = loginManager ??
                throw new ArgumentNullException(nameof(loginManager));
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public SessionLoginResult LoginAndInitializeSession(LoginArgs args)
        {
            return ExecuteService(
                LOG_CTX_LOGIN_INIT,
                () =>
                {
                    EnsureArgsNotNull(args);

                    SessionLoginResult loginResult = loginManager.Login(args);

                    if (loginResult == null || !loginResult.IsSuccess || loginResult.Profile == null)
                    {
                        return loginResult ?? SessionLoginResult.CreateFailed(DEFAULT_FAILED_STATUS);
                    }

                    long userId = loginResult.Profile.UserId;
                    EnsureValidUserIdOrThrow(userId);

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
                    {
                        bool sessionsTerminated = unitOfWork.Matches.ForceLeaveAllMatchesForUser(userId);

                        if (!sessionsTerminated)
                        {
                            Logger.WarnFormat("{0}: could not terminate active sessions for userId '{1}'.",
                                LOG_CTX_LOGIN_INIT, userId);
                        }

                        bool markedActive = unitOfWork.UserAccounts.MarkUserProfileActive(userId);

                        if (!markedActive)
                        {
                            Logger.ErrorFormat("{0}: could not mark profile active for userId '{1}'.",
                                LOG_CTX_LOGIN_INIT, userId);

                            throw FaultsFactory.Create(
                                LoginCoordinatorFaultKeys.CODE_PROFILE_MARK_ACTIVE_FAILED,
                                LoginCoordinatorFaultKeys.MSG_PROFILE_MARK_ACTIVE_FAILED,
                                LoginCoordinatorFaultKeys.FALLBACK_PROFILE_MARK_ACTIVE_FAILED);
                        }

                        unitOfWork.Flush();
                        transaction.Commit();
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
                    EnsureValidUserIdOrThrow(userProfileId);

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
                    {
                        bool sessionsTerminated = unitOfWork.Matches.ForceLeaveAllMatchesForUser(userProfileId);

                        if (!sessionsTerminated)
                        {
                            Logger.WarnFormat("{0}: could not terminate active sessions for userProfileId '{1}'.",
                                LOG_CTX_LOGOUT, userProfileId);

                            throw FaultsFactory.Create(
                                LoginCoordinatorFaultKeys.CODE_LOGOUT_TERMINATE_SESSIONS_FAILED,
                                LoginCoordinatorFaultKeys.MSG_LOGOUT_TERMINATE_SESSIONS_FAILED,
                                LoginCoordinatorFaultKeys.FALLBACK_LOGOUT_TERMINATE_SESSIONS_FAILED);
                        }

                        bool markedInactive = unitOfWork.UserAccounts.MarkUserProfileInactive(userProfileId);

                        if (!markedInactive)
                        {
                            Logger.WarnFormat("{0}: could not mark profile inactive for userProfileId '{1}'.",
                                LOG_CTX_LOGOUT, userProfileId);

                            throw FaultsFactory.Create(
                                LoginCoordinatorFaultKeys.CODE_LOGOUT_MARK_INACTIVE_FAILED,
                                LoginCoordinatorFaultKeys.MSG_LOGOUT_MARK_INACTIVE_FAILED,
                                LoginCoordinatorFaultKeys.FALLBACK_LOGOUT_MARK_INACTIVE_FAILED);
                        }

                        unitOfWork.Flush();
                        transaction.Commit();

                        return true;
                    }
                });
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

        private static void EnsureValidUserIdOrThrow(long userId)
        {
            if (userId >= MIN_VALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(
                LoginCoordinatorFaultKeys.CODE_USER_ID_INVALID,
                LoginCoordinatorFaultKeys.MSG_USER_ID_INVALID,
                LoginCoordinatorFaultKeys.FALLBACK_USER_ID_INVALID);
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
