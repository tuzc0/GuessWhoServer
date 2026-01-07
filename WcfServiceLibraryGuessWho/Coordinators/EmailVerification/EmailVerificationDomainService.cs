using GuessWhoCore.Contracts.Faults;
using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServerDomain.Domain.Settings;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Text.RegularExpressions;

namespace GuessWhoServices.Coordinators.EmailVerification
{
    public sealed class EmailVerificationDomainServiceArgs
    {
        public EmailVerificationDomainServiceArgs(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IVerificationCodeService verificationCodeService,
            UserSecuritySettings userSecuritySettings,
            ILog logger)
        {
            UnitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            VerificationCodeService = verificationCodeService ?? 
                throw new ArgumentNullException(nameof(verificationCodeService));
            UserSecuritySettings = userSecuritySettings ?? 
                throw new ArgumentNullException(nameof(userSecuritySettings));
            Logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));
        }

        public IGuessWhoUnitOfWorkFactory UnitOfWorkFactory { get; }
        public IVerificationCodeService VerificationCodeService { get; }
        public UserSecuritySettings UserSecuritySettings { get; }
        public ILog Logger { get; }
    }

    public sealed class EmailVerificationDomainService : IEmailVerificationDomainService
    {
        private const string LOG_CTX_MATCH_FAILED = "EmailVerificationDomainService.TokenMatchFailed";
        private const string LOG_CTX_RESEND_LIMIT = "EmailVerificationDomainService.ResendLimit";
        private const string LOG_CTX_FORMAT_INVALID = "EmailVerificationDomainService.CodeFormatInvalid";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly UserSecuritySettings userSecuritySettings;
        private readonly ILog logger;

        public EmailVerificationDomainService(EmailVerificationDomainServiceArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            unitOfWorkFactory = args.UnitOfWorkFactory;
            verificationCodeService = args.VerificationCodeService;
            userSecuritySettings = args.UserSecuritySettings;
            logger = args.Logger;
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

            logger.WarnFormat("{0}: invalid code format for accountId '{1}'.", LOG_CTX_FORMAT_INVALID, accountId);

            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_INVALID_FORMAT);
        }

        public void ValidateTokenMatchOrThrow(ValidateTokenMatchArgs tokenMatchArgs)
        {
            if (tokenMatchArgs == null)
            {
                throw new ArgumentNullException(nameof(tokenMatchArgs));
            }

            if (tokenMatchArgs.Token == null || !tokenMatchArgs.Token.IsValid)
            {
                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_EXPIRED);
            }

            byte[] inputHash = verificationCodeService.ComputeSha256Hash(tokenMatchArgs.TrimmedCode);

            if (verificationCodeService.AreEqualConstantTime(inputHash, tokenMatchArgs.Token.CodeHash))
            {
                return;
            }

            logger.WarnFormat("{0}: incorrect code for accountId '{1}'.", LOG_CTX_MATCH_FAILED, tokenMatchArgs.AccountId);

            PersistFailedAttemptIndependently(tokenMatchArgs);

            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_INCORRECT);
        }

        public void ValidateResendLimitsOrThrow(long accountId, DateTime nowUtc)
        {
            using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
            {
                ValidateResendLimitsOrThrow(accountId, nowUtc, unitOfWork);
            }
        }

        public void ValidateResendLimitsOrThrow(long accountId, DateTime nowUtc, IGuessWhoUnitOfWork unitOfWork)
        {
            if (unitOfWork == null)
            {
                throw new ArgumentNullException(nameof(unitOfWork));
            }

            var query = new ResendLimitsQuery(
                accountId,
                nowUtc,
                userSecuritySettings.ResendCooldownSeconds,
                userSecuritySettings.ResendHourlyMaxTokens);

            EmailVerificationResendLimitsResult resendLimits = unitOfWork.EmailVerification.GetEmailVerificationResendLimits(query);

            if (resendLimits.IsPerMinuteCooldownActive)
            {
                logger.WarnFormat("{0}: blocked by per-minute limit for accountId '{1}'.", LOG_CTX_RESEND_LIMIT, accountId);

                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_RESEND_TOO_FREQUENT);
            }

            if (!resendLimits.IsWithinHourlyLimit)
            {
                logger.WarnFormat("{0}: blocked by hourly limit for accountId '{1}'.", LOG_CTX_RESEND_LIMIT, accountId);

                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_RESEND_HOURLY_LIMIT_EXCEEDED);
            }
        }

        private void PersistFailedAttemptIndependently(ValidateTokenMatchArgs tokenMatchArgs)
        {
            var incrementArgs = new IncrementFailedAttemptArgs(
                tokenMatchArgs.Token.TokenId,
                tokenMatchArgs.NowUtc,
                userSecuritySettings.MaxFailedAttempts);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            unitOfWork.EmailVerification.IncrementFailedAttemptsAndMaybeExpire(incrementArgs);
            unitOfWork.Flush();
        }
    }
}
