using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWho.Services.Security;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoServices.Repositories.Interfaces;
using log4net;
using System;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
using WcfServiceLibraryGuessWho.Services.Settings;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class PasswordRecoveryManager : IPasswordRecoveryManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PasswordRecoveryManager));

        private readonly IUserAccountRepository accountRepository;
        private readonly IEmailVerificationRepository emailVerificationRepository;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IVerificationEmailDispatcher verificationEmailDispatcher;
        private readonly UserSecuritySettings userSecuritySettings;

        public PasswordRecoveryManager(
            IUserAccountRepository accountRepository,
            IEmailVerificationRepository emailVerificationRepository,
            IVerificationCodeService verificationCodeService,
            IVerificationEmailDispatcher verificationEmailDispatcher,
            UserSecuritySettings userSecuritySettings)
        {
            this.accountRepository = accountRepository ?? 
                throw new ArgumentNullException(nameof(accountRepository));
            this.emailVerificationRepository = emailVerificationRepository ?? 
                throw new ArgumentNullException(nameof(emailVerificationRepository));
            this.verificationCodeService = verificationCodeService ?? 
                throw new ArgumentNullException(nameof(verificationCodeService));
            this.verificationEmailDispatcher = verificationEmailDispatcher ?? 
                throw new ArgumentNullException(nameof(verificationEmailDispatcher));
            this.userSecuritySettings = userSecuritySettings ?? 
                throw new ArgumentNullException(nameof(userSecuritySettings));
        }

        public PasswordRecoveryResponse SendRecoveryPassword(PasswordRecoveryRequest request)
        {
            if (request == null)
            {
                Logger.Warn("SendRecoveryPassword request is null.");
                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_REQUEST_NULL,
                    PasswordRecoveryFaults.FAULT_MESSAGE_REQUEST_NULL);
            }

            string normalizedEmail = NormalizeEmail(request.Email);
            DateTime nowUtc = DateTime.UtcNow;

            Logger.InfoFormat("Password recovery requested for email '{0}'.", normalizedEmail);

            long accountId = accountRepository.GetAccountIdByEmail(normalizedEmail);

            if (accountId <= 0)
            {
                Logger.WarnFormat(
                    "Password recovery: Account not found for email '{0}'. Returning ambiguous success.",
                    normalizedEmail);

                return new PasswordRecoveryResponse
                {
                    Success = true,
                    Message = PasswordRecoveryFaults.RECOVERY_AMBIGUOUS_MESSAGE
                };
            }

            EnsureResendLimitsOrThrow(accountId, normalizedEmail, nowUtc);

            VerificationCodeResult verificationCodeResult = verificationCodeService.CreateVerificationCodeOrFault();

            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = accountId,
                CodeHash = verificationCodeResult.HashCode,
                NowUtc = nowUtc,
                LifeSpan = userSecuritySettings.VerificationCodeLifetime
            };

            emailVerificationRepository.AddVerificationToken(createTokenArgs);

            verificationEmailDispatcher.SendVerificationEmailOrThrow(
                normalizedEmail,
                verificationCodeResult.PlainCode);

            return new PasswordRecoveryResponse
            {
                Success = true,
                Message = PasswordRecoveryFaults.PASSWORD_RECOVERY_SUCCESS_MESSAGE
            };
        }

        public bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request)
        {
            if (request == null)
            {
                Logger.Warn("UpdatePasswordWithVerificationCode request is null.");
                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_REQUEST_NULL,
                    PasswordRecoveryFaults.FAULT_MESSAGE_REQUEST_NULL);
            }

            string normalizedEmail = NormalizeEmail(request.Email);
            string verificationCode = (request.VerificationCode ?? string.Empty).Trim();
            string newPassword = request.NewPassword ?? string.Empty;
            DateTime nowUtc = DateTime.UtcNow;

            long accountId = accountRepository.GetAccountIdByEmail(normalizedEmail);

            if (accountId <= 0)
            {
                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_ACCOUNT_NOT_FOUND,
                    PasswordRecoveryFaults.FAULT_MESSAGE_ACCOUNT_NOT_FOUND);
            }

            var emailToken = emailVerificationRepository.GetLatestTokenByAccountId(accountId, nowUtc);

            if (!emailToken.IsValid)
            {
                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    PasswordRecoveryFaults.FAULT_MESSAGE_VERIFICATION_CODE_EXPIRED);
            }

            byte[] inputHash = verificationCodeService.ComputeSha256Hash(verificationCode);

            if (!verificationCodeService.AreEqualConstantTime(inputHash, emailToken.CodeHash))
            {
                var incrementArgs = new IncrementFailedAttemptArgs(
                    emailToken.TokenId,
                    nowUtc,
                    userSecuritySettings.MaxFailedAttempts);

                emailVerificationRepository.IncrementFailedAttemptsAndMaybeExpire(incrementArgs);

                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    PasswordRecoveryFaults.FAULT_MESSAGE_VERIFICATION_CODE_INVALID);
            }

            emailVerificationRepository.ConsumeToken(emailToken.TokenId);

            byte[] newPasswordHash = PasswordHasher.HashPassword(newPassword);

            bool passwordUpdated = accountRepository.UpdatePasswordOnly(accountId, newPasswordHash);

            if (passwordUpdated)
            {
                return true;
            }

            throw Faults.Create(
                PasswordRecoveryFaults.FAULT_CODE_UNEXPECTED_ERROR,
                PasswordRecoveryFaults.FAULT_MESSAGE_UPDATE_PASSWORD_DB_FAILED);
        }

        private void EnsureResendLimitsOrThrow(long accountId, string normalizedEmail, DateTime nowUtc)
        {
            var resendLimitsQuery = new ResendLimitsQuery(accountId, nowUtc);
            var resendLimits = emailVerificationRepository.GetEmailVerificationResendLimits(resendLimitsQuery);

            if (resendLimits.IsPerMinuteCooldownActive)
            {
                Logger.WarnFormat(
                    "Password recovery blocked by per-minute limit for email '{0}'.",
                    normalizedEmail);

                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_RESEND_TOO_FREQUENT,
                    PasswordRecoveryFaults.FAULT_MESSAGE_RESEND_TOO_FREQUENT);
            }

            if (!resendLimits.IsWithinHourlyLimit)
            {
                Logger.WarnFormat(
                    "Password recovery blocked by hourly limit for email '{0}'.",
                    normalizedEmail);

                throw Faults.Create(
                    PasswordRecoveryFaults.FAULT_CODE_HOURLY_LIMIT_EXCEEDED,
                    PasswordRecoveryFaults.FAULT_MESSAGE_HOURLY_LIMIT_EXCEEDED);
            }
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
