using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Dtos;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Models.Session;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Errors;
using GuessWhoServices.Infrastructure.Session;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Coordinators
{
    public sealed class LoginCoordinator : ManagerBase, ILoginCoordinator
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(LoginCoordinator));

        private const string LOG_CTX_LOGIN_INIT = "LoginCoordinator.LoginAndInitializeSession";
        private const string LOG_CTX_LOGOUT = "LoginCoordinator.Logout";

        private const int MIN_VALID_ID = 1;

        private const LoginStatus DEFAULT_FAILED_STATUS = LoginStatus.InvalidCredentials;

        private readonly ILoginManager loginManager;
        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IOnlineUserRegistry onlineUserRegistry;

        public LoginCoordinator(
            ILoginManager loginManager,
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IOnlineUserRegistry onlineUserRegistry)
        {
            this.loginManager = loginManager ?? throw new ArgumentNullException(nameof(loginManager));
            this.unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.onlineUserRegistry = onlineUserRegistry ?? throw new ArgumentNullException(nameof(onlineUserRegistry));
        }

        public SessionLoginResult LoginAndInitializeSession(LoginSessionArgs args)
        {
            return ExecuteService(
                LOG_CTX_LOGIN_INIT,
                () =>
                {
                    EnsureArgsNotNull(args);

                    SessionLoginResult loginResult = loginManager.Login(args.LoginArgs);

                    if (loginResult == null || !loginResult.IsSuccess || loginResult.Profile == null)
                    {
                        return loginResult ?? SessionLoginResult.CreateFailed(DEFAULT_FAILED_STATUS);
                    }

                    long userId = loginResult.Profile.UserId;
                    EnsureValidUserIdOrThrow(userId);

                    bool isLoginCommitted = false;

                    RegisterOrReplaceResult presenceResult = onlineUserRegistry.RegisterOrReplace(
                        new RegisterOrReplaceRequest(userId, args.Channel));

                    try
                    {
                        if (!presenceResult.IsRegistered)
                        {
                            Logger.WarnFormat(
                                "{0}: blocked login (account in use). userId='{1}'.",
                                LOG_CTX_LOGIN_INIT,
                                userId);

                            throw FaultsFactory.Create(LoginFaultKeys.CODE_PROFILE_ALREADY_ACTIVE);
                        }

                        using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                        using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
                        {
                            bool sessionsTerminated = unitOfWork.Matches.ForceLeaveAllMatchesForUser(userId);

                            if (!sessionsTerminated)
                            {
                                Logger.WarnFormat(
                                    "{0}: could not terminate active match sessions for userId '{1}'.",
                                    LOG_CTX_LOGIN_INIT,
                                    userId);
                            }

                            bool markedActive = unitOfWork.UserAccounts.MarkUserProfileActive(userId);

                            if (!markedActive)
                            {
                                Logger.ErrorFormat(
                                    "{0}: could not mark profile active for userId '{1}'.",
                                    LOG_CTX_LOGIN_INIT,
                                    userId);

                                throw FaultsFactory.Create(LoginCoordinatorFaultKeys.CODE_PROFILE_MARK_ACTIVE_FAILED);
                            }

                            unitOfWork.Flush();
                            transaction.Commit();
                        }

                        isLoginCommitted = true;

                        return loginResult;
                    }
                    finally
                    {
                        if (!isLoginCommitted)
                        {
                            onlineUserRegistry.Remove(new RemoveRequest(userId, args.Channel));
                        }
                    }
                });
        }

        public bool Logout(LogoutSessionArgs args)
        {
            return ExecuteService(
                LOG_CTX_LOGOUT,
                () =>
                {
                    EnsureArgsNotNull(args);

                    EnsureValidUserIdOrThrow(args.UserProfileId);

                    onlineUserRegistry.Remove(new RemoveRequest(args.UserProfileId, args.Channel));

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
                    {
                        bool sessionsTerminated = unitOfWork.Matches.ForceLeaveAllMatchesForUser(args.UserProfileId);

                        if (!sessionsTerminated)
                        {
                            Logger.WarnFormat(
                                "{0}: could not terminate active match sessions for userProfileId '{1}'.",
                                LOG_CTX_LOGOUT,
                                args.UserProfileId);
                        }

                        bool markedInactive = unitOfWork.UserAccounts.MarkUserProfileInactive(args.UserProfileId);

                        if (!markedInactive)
                        {
                            Logger.WarnFormat(
                                "{0}: could not mark profile inactive for userProfileId '{1}'.",
                                LOG_CTX_LOGOUT,
                                args.UserProfileId);

                            throw FaultsFactory.Create(LoginCoordinatorFaultKeys.CODE_LOGOUT_MARK_INACTIVE_FAILED);
                        }

                        unitOfWork.Flush();
                        transaction.Commit();

                        return true;
                    }
                });
        }

        public void TouchPresence(long userId)
        {
            const int MIN_VALID_ID = 1;

            if (userId < MIN_VALID_ID)
            {
                throw FaultsFactory.Create(LoginCoordinatorFaultKeys.CODE_USER_ID_INVALID);
            }

            onlineUserRegistry.Touch(new TouchRequest(userId));
        }

        private static void EnsureArgsNotNull(object args)
        {
            if (args != null)
            {
                return;
            }

            throw FaultsFactory.Create(LoginFaultKeys.CODE_REQUEST_NULL);
        }

        private static void EnsureValidUserIdOrThrow(long userId)
        {
            if (userId >= MIN_VALID_ID)
            {
                return;
            }

            throw FaultsFactory.Create(LoginCoordinatorFaultKeys.CODE_USER_ID_INVALID);
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
