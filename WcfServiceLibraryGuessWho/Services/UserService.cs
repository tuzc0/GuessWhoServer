using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Coordinators;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
using WcfServiceLibraryGuessWho.Coordinators.InternalDtos;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class UserService : ServiceBase, IUserService
    {
        protected override ILog Logger { get; } = LogManager.GetLogger(typeof(UserService));

        private const string LOG_CTX_REGISTER = "UserService.RegisterUser";
        private const string LOG_CTX_CONFIRM_EMAIL = "UserService.ConfirmEmailAddressWithVerificationCode";
        private const string LOG_CTX_RESEND_EMAIL = "UserService.ResendEmailVerificationCode";
        private const string LOG_CTX_SEND_RECOVERY = "UserService.SendPasswordRecoveryCode";
        private const string LOG_CTX_UPDATE_PASSWORD = "UserService.UpdatePasswordWithVerificationCode";

        private readonly IUserRegistrationManager userRegistrationManager;
        private readonly IEmailVerificationManager emailVerificationManager;
        private readonly IPasswordRecoveryManager passwordRecoveryManager;

        public UserService(
            IUserRegistrationManager userRegistrationManager,
            IEmailVerificationManager emailVerificationManager,
            IPasswordRecoveryManager passwordRecoveryManager)
        {
            this.userRegistrationManager = userRegistrationManager ??
                throw new ArgumentNullException(nameof(userRegistrationManager));
            this.emailVerificationManager = emailVerificationManager ??
                throw new ArgumentNullException(nameof(emailVerificationManager));
            this.passwordRecoveryManager = passwordRecoveryManager ??
                throw new ArgumentNullException(nameof(passwordRecoveryManager));
        }

        public RegisterResponse RegisterUser(RegisterRequest request)
        {
            return ExecuteService(
                LOG_CTX_REGISTER,
                () =>
                {
                    EnsureRequestNotNull(request);

                    RegisterUserArgs registrationArgs = BuildRegistrationArgs(request);
                    RegisterResult result = userRegistrationManager.RegisterUser(registrationArgs);

                    return new RegisterResponse
                    {
                        AccountId = result.AccountId,
                        UserId = result.UserId,
                        Email = result.Email,
                        DisplayName = result.DisplayName,
                        EmailVerificationRequired = result.EmailVerificationRequired
                    };
                });
        }

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {
            return ExecuteService(
                LOG_CTX_CONFIRM_EMAIL,
                () =>
                {
                    EnsureRequestNotNull(request);
                    return emailVerificationManager.ConfirmEmailAddressWithVerificationCode(request);
                });
        }

        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            ExecuteService(
                LOG_CTX_RESEND_EMAIL,
                () =>
                {
                    EnsureRequestNotNull(request);
                    emailVerificationManager.ResendEmailVerificationCode(request);
                });
        }

        public PasswordRecoveryResponse SendPasswordRecoveryCode(PasswordRecoveryRequest request)
        {
            return ExecuteService(
                LOG_CTX_SEND_RECOVERY,
                () =>
                {
                    EnsureRequestNotNull(request);
                    return passwordRecoveryManager.SendRecoveryPassword(request);
                });
        }

        public bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request)
        {
            return ExecuteService(
                LOG_CTX_UPDATE_PASSWORD,
                () =>
                {
                    EnsureRequestNotNull(request);
                    return passwordRecoveryManager.UpdatePasswordWithVerificationCode(request);
                });
        }

        private static void EnsureRequestNotNull(object request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                InfrastructureFaultKeys.CODE_REQUEST_NULL,
                InfrastructureFaultKeys.MSG_REQUEST_NULL,
                InfrastructureFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static RegisterUserArgs BuildRegistrationArgs(RegisterRequest request)
        {
            string normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string normalizedDisplayName = (request.DisplayName ?? string.Empty).Trim();
            string safePassword = request.Password ?? string.Empty;

            return new RegisterUserArgs(
                normalizedEmail,
                normalizedDisplayName,
                safePassword,
                DateTime.UtcNow);
        }
    }
}
