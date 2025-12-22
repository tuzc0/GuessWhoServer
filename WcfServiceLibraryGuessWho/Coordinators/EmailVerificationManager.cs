using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWho.Services.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using GuessWhoServices.Repositories.Interfaces;
using log4net;
using System;
using System.Text.RegularExpressions;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
using WcfServiceLibraryGuessWho.Services.Settings;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class EmailVerificationManager : IEmailVerificationManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EmailVerificationManager));

        private readonly IUserAccountRepository accountRepository;
        private readonly IEmailVerificationRepository emailVerificationRepository;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IVerificationEmailDispatcher verificationEmailDispatcher;
        private readonly UserSecuritySettings userSecuritySettings;

        public EmailVerificationManager(
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

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {
            EnsureRequestIsNotNull(request);

            DateTime nowUtc = DateTime.UtcNow;
            string trimmedCode = (request.Code ?? string.Empty).Trim();

            EnsureVerificationCodeFormatIsValid(request.AccountId, trimmedCode);

            AccountDto account = LoadUnverifiedAccountOrSkip(request.AccountId);

            if (account == null)
            {
                return new VerifyEmailResponse { Success = true };
            }

            EmailVerificationTokenDto activeToken =
                emailVerificationRepository.GetLatestTokenByAccountId(request.AccountId, nowUtc);

            if (!activeToken.IsValid)
            {
                ThrowInvalidOrExpiredVerificationCodeFault(request.AccountId, nowUtc);
            }

            EnsureVerificationCodeMatchesOrThrow(request.AccountId, trimmedCode, activeToken, nowUtc);

            ConsumeTokenOrThrow(activeToken.TokenId);

            MarkEmailVerifiedOrThrow(request.AccountId, nowUtc);

            return new VerifyEmailResponse { Success = true };
        }

        private static void EnsureRequestIsNotNull(VerifyEmailRequest request)
        {
            if (request != null)
            {
                return;
            }

            Logger.Warn("ConfirmEmailAddressWithVerificationCode failed: request is null.");

            throw Faults.Create(
                EmailVerificationFaults.FAULT_CODE_INVALID_REQUEST,
                EmailVerificationFaults.FAULT_MESSAGE_INVALID_REQUEST);
        }

        private void EnsureVerificationCodeFormatIsValid(long accountId, string trimmedCode)
        {
            bool isValidFormat = Regex.IsMatch(
                trimmedCode,
                userSecuritySettings.VerificationCodePattern,
                RegexOptions.None,
                userSecuritySettings.RegexTimeout);

            if (isValidFormat)
            {
                return;
            }

            Logger.WarnFormat(
                "ConfirmEmailAddressWithVerificationCode failed: invalid code format for accountId '{0}'.",
                accountId);

            throw Faults.Create(
                EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INVALID_FORMAT);
        }

        private void ThrowInvalidOrExpiredVerificationCodeFault(long accountId, DateTime nowUtc)
        {
            EmailVerificationTokenDto lastToken =
                emailVerificationRepository.GetLatestTokenStatusByAccountId(accountId);

            if (!lastToken.IsValid)
            {
                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_EXPIRED_OR_MISSING);
            }

            if (lastToken.ConsumedUtc.HasValue)
            {
                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_ALREADY_USED);
            }

            if (lastToken.ExpiresUtc < nowUtc)
            {
                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_EXPIRED_OR_MISSING);
            }

            throw Faults.Create(
                EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_EXPIRED_OR_MISSING);
        }

        private void EnsureVerificationCodeMatchesOrThrow(
            long accountId,
            string trimmedCode,
            EmailVerificationTokenDto token,
            DateTime nowUtc)
        {
            byte[] inputHash = verificationCodeService.ComputeSha256Hash(trimmedCode);

            if (verificationCodeService.AreEqualConstantTime(inputHash, token.CodeHash))
            {
                return;
            }

            Logger.WarnFormat(
                "ConfirmEmailAddressWithVerificationCode failed: incorrect code for accountId '{0}'.",
                accountId);

            var incrementArgs = new IncrementFailedAttemptArgs(
                token.TokenId,
                nowUtc,
                userSecuritySettings.MaxFailedAttempts);

            emailVerificationRepository.IncrementFailedAttemptsAndMaybeExpire(incrementArgs);

            throw Faults.Create(
                EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_INCORRECT);
        }

        private void ConsumeTokenOrThrow(Guid tokenId)
        {
            int consumedRows = emailVerificationRepository.ConsumeToken(tokenId);

            if (consumedRows > 0)
            {
                return;
            }

            Logger.WarnFormat(
                "ConfirmEmailAddressWithVerificationCode failed: token already consumed or not found for tokenId '{0}'.",
                tokenId);

            throw Faults.Create(
                EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_CODE_ALREADY_USED);
        }

        private void MarkEmailVerifiedOrThrow(long accountId, DateTime nowUtc)
        {
            bool updated = accountRepository.MarkEmailVerified(accountId, nowUtc);

            if (updated)
            {
                return;
            }

            Logger.WarnFormat(
                "ConfirmEmailAddressWithVerificationCode failed: could not mark email as verified for accountId '{0}'.",
                accountId);

            throw Faults.Create(
                EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_FAILED,
                EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_FAILED);
        }


        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_INVALID_REQUEST,
                    EmailVerificationFaults.FAULT_MESSAGE_INVALID_REQUEST);
            }

            DateTime nowUtc = DateTime.UtcNow;

            AccountDto account = LoadUnverifiedAccountOrSkip(request.AccountId);

            if (account == null)
            {
                return;
            }

            var resendLimitsQuery = new ResendLimitsQuery(request.AccountId, nowUtc);
            var resendLimits = emailVerificationRepository.GetEmailVerificationResendLimits(resendLimitsQuery);

            if (resendLimits.IsPerMinuteCooldownActive)
            {
                Logger.WarnFormat(
                    "ResendEmailVerificationCode blocked by per-minute limit for accountId '{0}'.",
                    request.AccountId);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_TOO_FREQUENT);
            }

            if (!resendLimits.IsWithinHourlyLimit)
            {
                Logger.WarnFormat(
                    "ResendEmailVerificationCode blocked by hourly limit for accountId '{0}'.",
                    request.AccountId);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED,
                    EmailVerificationFaults.FAULT_MESSAGE_EMAIL_VERIFICATION_RESEND_HOURLY_LIMIT_EXCEEDED);
            }

            VerificationCodeResult verificationCode = verificationCodeService.CreateVerificationCodeOrFault();

            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = request.AccountId,
                CodeHash = verificationCode.HashCode,
                NowUtc = nowUtc,
                LifeSpan = userSecuritySettings.VerificationCodeLifetime
            };

            emailVerificationRepository.AddVerificationToken(createTokenArgs);

            verificationEmailDispatcher.SendVerificationEmailOrThrow(account.Email, verificationCode.PlainCode);
        }

        private AccountDto LoadUnverifiedAccountOrSkip(long accountId)
        {
            AccountDto account = accountRepository.GetAccountByIdAccount(accountId);

            if (!account.IsValid)
            {
                Logger.WarnFormat(
                    "LoadUnverifiedAccountOrSkip failed: account not found for accountId '{0}'.",
                    accountId);

                throw Faults.Create(
                    EmailVerificationFaults.FAULT_CODE_ACCOUNT_NOT_FOUND,
                    EmailVerificationFaults.FAULT_MESSAGE_ACCOUNT_NOT_FOUND);
            }

            if (account.IsEmailVerified)
            {
                Logger.InfoFormat(
                    "LoadUnverifiedAccountOrSkip skipped: email already verified for accountId '{0}'.",
                    accountId);

                return null;
            }

            return account;
        }
    }
}
