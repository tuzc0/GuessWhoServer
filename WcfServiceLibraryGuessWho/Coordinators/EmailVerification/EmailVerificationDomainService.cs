using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using GuessWhoServerDomain.Domain.Settings;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Text.RegularExpressions;

namespace WcfServiceLibraryGuessWho.Coordinators.EmailVerification
{
    public sealed class EmailVerificationDomainService : IEmailVerificationDomainService
    {
        private const string LOG_CTX_MATCH_FAILED = "EmailVerificationDomainService.TokenMatchFailed";

        private const string LOG_CTX_RESEND_LIMIT = "EmailVerificationDomainService.ResendLimit";

        private const string LOG_CTX_FORMAT_INVALID = "EmailVerificationDomainService.CodeFormatInvalid";

        private readonly IEmailVerificationRepository emailVerificationRepository;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly UserSecuritySettings userSecuritySettings;
        private readonly ILog logger; 

        public EmailVerificationDomainService(
            IEmailVerificationRepository emailVerificationRepository,
            IVerificationCodeService verificationCodeService,
            UserSecuritySettings userSecuritySettings,
            ILog logger)
        {
            this.emailVerificationRepository = emailVerificationRepository ?? 
                throw new ArgumentNullException(nameof(emailVerificationRepository));
            this.verificationCodeService = verificationCodeService ?? 
                throw new ArgumentNullException( nameof(verificationCodeService));
            this.userSecuritySettings = userSecuritySettings ?? 
                throw new ArgumentNullException(nameof(userSecuritySettings));
            this.logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));
        }

        public TimeSpan GetVerificationCodeLifetime()
        {
            return userSecuritySettings.VerificationCodeLifetime;
        }

        public void ValidateVerificationCodeFormatOrThrow(long accountId, string trimmedCode)
        {
            bool isValidFormat = Regex.IsMatch(
                trimmedCode ?? string.Empty,
                userSecuritySettings.VerificationCodePattern, 
                RegexOptions.None,
                userSecuritySettings.RegexTimeout);

            if (isValidFormat)
            {
                return;
            }

            logger.WarnFormat("{0}: invalid code format for accountId '{1}'.",
                LOG_CTX_FORMAT_INVALID, accountId);

            throw FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_CODE_INVALID_FORMAT,
                EmailVerificationFaultKeys.MSG_CODE_INVALID_FORMAT,
                EmailVerificationFaultKeys.FALLBACK_CODE_INVALID_FORMAT);
        }

        public void ValidateTokenMatchOrThrow(ValidateTokenMatchArgs tokenMatchArgs)
        {
            if (tokenMatchArgs.Token == null || !tokenMatchArgs.Token.IsValid)
            {
                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaultKeys.MSG_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaultKeys.FALLBACK_CODE_INVALID_OR_EXPIRED);
            }

            byte[] inputHash = verificationCodeService.ComputeSha256Hash(tokenMatchArgs.TrimmedCode);

            if (verificationCodeService.AreEqualConstantTime(inputHash, tokenMatchArgs.Token.CodeHash))
            {
                return;
            }

            logger.WarnFormat("{0}: incorrect code for accountId '{1}'.", 
                LOG_CTX_MATCH_FAILED, tokenMatchArgs.AccountId);

            var incrementArgs = new IncrementFailedAttemptArgs(
                tokenMatchArgs.Token.TokenId,
                tokenMatchArgs.NowUtc,
                userSecuritySettings.MaxFailedAttempts);

            emailVerificationRepository.IncrementFailedAttemptsAndMaybeExpire(incrementArgs);

            throw FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaultKeys.MSG_CODE_INCORRECT,
                EmailVerificationFaultKeys.FALLBACK_CODE_INCORRECT);
        }

        public void ValidateResendLimitsOrThrow(long accountId, DateTime nowUtc)
        {
            var resendLimitsQuery = new ResendLimitsQuery(accountId, nowUtc, userSecuritySettings.ResendCooldownSeconds, userSecuritySettings.ResendHourlyMaxTokens);

            var resendLimits = emailVerificationRepository.GetEmailVerificationResendLimits(resendLimitsQuery);

            if (resendLimits.IsPerMinuteCooldownActive)
            {
                logger.WarnFormat("{0}: blocked by per-minute limit for accountId '{1}'",
                    LOG_CTX_RESEND_LIMIT,
                    accountId);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_RESEND_TOO_FREQUENT,
                    EmailVerificationFaultKeys.MSG_RESEND_TOO_FREQUENT,
                    EmailVerificationFaultKeys.FALLBACK_RESEND_TOO_FREQUENT);
            }

            if (!resendLimits.IsWithinHourlyLimit)
            {
                logger.WarnFormat(
                    "{0}: blocked by hourly limit for accountId '{1}'.",
                    LOG_CTX_RESEND_LIMIT,
                    accountId);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_RESEND_HOURLY_LIMIT_EXCEEDED,
                    EmailVerificationFaultKeys.MSG_RESEND_HOURLY_LIMIT_EXCEEDED,
                    EmailVerificationFaultKeys.FALLBACK_RESEND_HOURLY_LIMIT_EXCEEDED);
            }
        }
    }
}
