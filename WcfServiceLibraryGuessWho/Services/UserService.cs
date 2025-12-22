using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoContracts.Services;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Coordinators;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace GuessWho.Services.WCF.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = false, InstanceContextMode = InstanceContextMode.PerCall)]
    public sealed class UserService : IUserService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UserService));

        private const string FAULT_CODE_REQUEST_NULL = "USER_REQUEST_NULL";
        private const string FAULT_MESSAGE_REQUEST_NULL = "The request cannot be null.";

        private const string FAULT_CODE_REGISTER_INVALID_INPUT = "USER_REGISTER_INVALID_INPUT";
        private const string FAULT_MESSAGE_REGISTER_INVALID_INPUT =
            "Some registration fields are invalid. Please check your information and try again.";

        private const string FAULT_CODE_REGISTER_FAILED = "USER_REGISTER_FAILED";
        private const string FAULT_MESSAGE_REGISTER_FAILED =
            "Your account could not be created. Please try again later.";

        private readonly IUserRegistrationManager userRegistrationManager;
        private readonly IEmailVerificationManager emailVerificationManager;
        private readonly IPasswordRecoveryManager passwordRecoveryManager;
        private readonly IUserFaultMapper userFaultMapper;

        public UserService(
            IUserRegistrationManager userRegistrationManager,
            IEmailVerificationManager emailVerificationManager,
            IPasswordRecoveryManager passwordRecoveryManager,
            IUserFaultMapper userFaultMapper)
        {
            this.userRegistrationManager = userRegistrationManager ??
                throw new ArgumentNullException(nameof(userRegistrationManager));
            this.emailVerificationManager = emailVerificationManager ??
                throw new ArgumentNullException(nameof(emailVerificationManager));
            this.passwordRecoveryManager = passwordRecoveryManager ??
                throw new ArgumentNullException(nameof(passwordRecoveryManager));
            this.userFaultMapper = userFaultMapper ??
                throw new ArgumentNullException(nameof(userFaultMapper));
        }

        public RegisterResponse RegisterUser(RegisterRequest request)
        {
            if (request == null)
            {
                Logger.Warn("RegisterUser request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            RegisterUserArgs registrationArgs = BuildRegistrationArgs(request);

            try
            {
                RegisterResult registerResult = userRegistrationManager.RegisterUser(registrationArgs);

                return new RegisterResponse
                {
                    AccountId = registerResult.AccountId,
                    UserId = registerResult.UserId,
                    Email = registerResult.Email,
                    DisplayName = registerResult.DisplayName,
                    EmailVerificationRequired = registerResult.EmailVerificationRequired
                };
            }
            catch (ArgumentException ex)
            {
                Logger.Warn("RegisterUser failed due to invalid input.", ex);

                throw Faults.Create(
                    FAULT_CODE_REGISTER_INVALID_INPUT,
                    FAULT_MESSAGE_REGISTER_INVALID_INPUT,
                    ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error("RegisterUser failed due to invalid operation.", ex);

                throw Faults.Create(
                    FAULT_CODE_REGISTER_FAILED,
                    FAULT_MESSAGE_REGISTER_FAILED,
                    ex);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (EmailSendException ex)
            {
                throw userFaultMapper.MapEmailSend(ex);
            }
            catch (DbUpdateException ex)
            {
                throw userFaultMapper.MapRegisterDb(ex);
            }
            catch (Exception ex)
            {
                throw userFaultMapper.MapRegisterUnknow(ex);
            }
        }

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {
            if (request == null)
            {
                Logger.Warn("ConfirmEmailAddressWithVerificationCode request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                return emailVerificationManager.ConfirmEmailAddressWithVerificationCode(request);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw userFaultMapper.MapEmailVerificationDb(ex);
            }
            catch (Exception ex)
            {
                throw userFaultMapper.MapEmailVerificationUnknown(ex);
            }
        }

        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            if (request == null)
            {
                Logger.Warn("ResendEmailVerificationCode request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                emailVerificationManager.ResendEmailVerificationCode(request);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw userFaultMapper.MapResendDb(ex);
            }
            catch (EmailSendException ex)
            {
                throw userFaultMapper.MapEmailSend(ex);
            }
            catch (Exception ex)
            {
                throw userFaultMapper.MapResendUnknown(ex);
            }
        }

        public PasswordRecoveryResponse SendPasswordRecoveryCode(PasswordRecoveryRequest request)
        {
            if (request == null)
            {
                Logger.Warn("SendPasswordRecoveryCode request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                return passwordRecoveryManager.SendRecoveryPassword(request);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw userFaultMapper.MapPasswordRecoveryDb(ex);
            }
            catch (EmailSendException ex)
            {
                throw userFaultMapper.MapEmailSend(ex);
            }
            catch (Exception ex)
            {
                throw userFaultMapper.MapPasswordRecoveryUnknown(ex);
            }
        }

        public bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request)
        {
            if (request == null)
            {
                Logger.Warn("UpdatePasswordWithVerificationCode request is null.");
                throw Faults.Create(
                    FAULT_CODE_REQUEST_NULL,
                    FAULT_MESSAGE_REQUEST_NULL);
            }

            try
            {
                return passwordRecoveryManager.UpdatePasswordWithVerificationCode(request);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw userFaultMapper.MapUpdatePasswordDb(ex);
            }
            catch (Exception ex)
            {
                throw userFaultMapper.MapUpdatePasswordUnknown(ex);
            }
        }

        private static RegisterUserArgs BuildRegistrationArgs(RegisterRequest request)
        {
            string normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            string normalizedDisplayName = (request.DisplayName ?? string.Empty).Trim();
            string safePassword = request.Password ?? string.Empty;
            DateTime nowUtc = DateTime.UtcNow;

            return new RegisterUserArgs(
                normalizedEmail,
                normalizedDisplayName,
                safePassword,
                nowUtc);
        }
    }
}
