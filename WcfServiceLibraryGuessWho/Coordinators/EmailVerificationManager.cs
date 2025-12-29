using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Communication.Email.Builders;
using WcfServiceLibraryGuessWho.Communication.Email.Builders.Context;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class EmailVerificationManager : ManagerBase, IEmailVerificationManager
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(EmailVerificationManager));

        private const string LOG_CTX_CONFIRM =
            "EmailVerificationManager.ConfirmEmailAddressWithVerificationCode";
        private const string LOG_CTX_RESEND =
            "EmailVerificationManager.ResendEmailVerificationCode";
        private const string LOG_CTX_REGEX_TIMEOUT =
            "EmailVerificationManager.RegexTimeout";

        private const int MIN_EXPIRATION_MINUTES = 1;

        private readonly IUserAccountRepository accountRepository;
        private readonly IEmailVerificationRepository emailVerificationRepository;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IEmailSender emailSender;
        private readonly VerificationCodeEmailBuilder verificationCodeEmailBuilder;
        private readonly IEmailVerificationDomainService domainService;

        public EmailVerificationManager(
            IUserAccountRepository accountRepository,
            IEmailVerificationRepository emailVerificationRepository,
            IVerificationCodeService verificationCodeService,
            IEmailSender emailSender,
            VerificationCodeEmailBuilder verificationCodeEmailBuilder,
            IEmailVerificationDomainService domainService)
        {
            this.accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
            this.emailVerificationRepository = emailVerificationRepository ??
                throw new ArgumentNullException(nameof(emailVerificationRepository));
            this.verificationCodeService = verificationCodeService ??
                throw new ArgumentNullException(nameof(verificationCodeService));
            this.emailSender = emailSender ??
                throw new ArgumentNullException(nameof(emailSender));
            this.verificationCodeEmailBuilder = verificationCodeEmailBuilder ??
                throw new ArgumentNullException(nameof(verificationCodeEmailBuilder));
            this.domainService = domainService ??
                throw new ArgumentNullException(nameof(domainService));
        }

        public VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request)
        {
            return ExecuteService(
                LOG_CTX_CONFIRM,
                () =>
                {
                    EnsureRequestIsNotNull(request);

                    DateTime nowUtc = DateTime.UtcNow;
                    string trimmedCode = (request.Code ?? string.Empty).Trim();

                    domainService.ValidateVerificationCodeFormatOrThrow(request.AccountId, trimmedCode);

                    AccountRecord account = LoadUnverifiedAccountOrSkip(request.AccountId);

                    if (account == null)
                    {
                        return new VerifyEmailResponse { Success = true };
                    }

                    EmailVerificationTokenRecord activeToken =
                        emailVerificationRepository.GetLatestActiveTokenByAccountId(request.AccountId, nowUtc);

                    if (activeToken == null || !activeToken.IsValid)
                    {
                        throw CreateInvalidOrExpiredVerificationCodeFault(request.AccountId, nowUtc);
                    }

                    var matchArgs = new ValidateTokenMatchArgs(
                        request.AccountId,
                        trimmedCode,
                        activeToken,
                        nowUtc);

                    domainService.ValidateTokenMatchOrThrow(matchArgs);

                    ConsumeTokenOrThrow(activeToken.TokenId);
                    MarkEmailVerifiedOrThrow(request.AccountId, nowUtc);

                    return new VerifyEmailResponse { Success = true };
                },
                customErrorHandler: HandleRegexTimeout);
        }

        public void ResendEmailVerificationCode(ResendVerificationRequest request)
        {
            ExecuteService(
                LOG_CTX_RESEND,
                () =>
                {
                    EnsureResendRequestIsNotNull(request);

                    DateTime nowUtc = DateTime.UtcNow;

                    AccountRecord account = LoadUnverifiedAccountOrSkip(request.AccountId);
                    
                    if (account == null)
                    {
                        return;
                    }

                    domainService.ValidateResendLimitsOrThrow(request.AccountId, nowUtc);

                    VerificationCodeResult verificationCode =
                        verificationCodeService.CreateVerificationCodeOrFault();

                    TimeSpan lifeTime = domainService.GetVerificationCodeLifetime();

                    var createTokenArgs = new CreateEmailTokenArgs
                    {
                        AccountId = request.AccountId,
                        CodeHash = verificationCode.HashCode,
                        NowUtc = nowUtc,
                        LifeSpan = lifeTime
                    };

                    int expirationMinutes = Math.Max(MIN_EXPIRATION_MINUTES,
                        (int)Math.Ceiling(domainService.GetVerificationCodeLifetime().TotalMinutes));

                    bool created = emailVerificationRepository.AddVerificationToken(createTokenArgs);

                    if (!created)
                    {
                        Logger.WarnFormat(
                            "{0}: token creation failed for accountId '{1}'.",
                            LOG_CTX_RESEND,
                            request.AccountId);

                        throw FaultsFactory.Create(
                            EmailVerificationFaultKeys.CODE_TOKEN_CREATION_FAILED,
                            EmailVerificationFaultKeys.MSG_TOKEN_CREATION_FAILED,
                            EmailVerificationFaultKeys.FALLBACK_TOKEN_CREATION_FAILED);
                    }

                    var message = verificationCodeEmailBuilder.Build(new VerificationCodeEmailContext(account.Email, verificationCode.PlainCode, expirationMinutes));

                    EmailSendResult sendResult = emailSender.Send(message);

                    if (!sendResult.IsSuccess)
                    {
                        Logger.WarnFormat(
                            "{0}: email send failed for accountId '{1}'. Status='{2}', Code='{3}'.",
                            LOG_CTX_RESEND,
                            request.AccountId,
                            sendResult.Status,
                            sendResult.ErrorCode);

                        if (sendResult.TechnicalException != null)
                        {
                            Logger.Error(LOG_CTX_RESEND + ": email technical exception.", sendResult.TechnicalException);
                        }

                        throw EmailFaultTranslator.ToInfrastructureEmailFault(sendResult);
                    }
                },
                customErrorHandler: HandleRegexTimeout);
        }

        private void HandleRegexTimeout(Exception ex)
        {
            if (ex is RegexMatchTimeoutException timeoutEx)
            {
                Logger.Error(LOG_CTX_REGEX_TIMEOUT, timeoutEx);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_UNEXPECTED_ERROR,
                    EmailVerificationFaultKeys.MSG_UNEXPECTED_ERROR,
                    EmailVerificationFaultKeys.FALLBACK_UNEXPECTED_ERROR,
                    timeoutEx);
            }
        }

        private static void EnsureRequestIsNotNull(VerifyEmailRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_REQUEST_NULL,
                EmailVerificationFaultKeys.MSG_REQUEST_NULL,
                EmailVerificationFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static void EnsureResendRequestIsNotNull(ResendVerificationRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_REQUEST_NULL,
                EmailVerificationFaultKeys.MSG_REQUEST_NULL,
                EmailVerificationFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private FaultException<ServiceFault> CreateInvalidOrExpiredVerificationCodeFault(long accountId, DateTime nowUtc)
        {
            EmailVerificationTokenRecord lastToken =
                emailVerificationRepository.GetLatestTokenStatusByAccountId(accountId);

            if (lastToken == null)
            {
                return FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaultKeys.MSG_CODE_EXPIRED_OR_MISSING,
                    EmailVerificationFaultKeys.FALLBACK_CODE_EXPIRED_OR_MISSING);
            }

            if (lastToken.ConsumedUtc.HasValue)
            {
                return FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaultKeys.MSG_CODE_ALREADY_USED,
                    EmailVerificationFaultKeys.FALLBACK_CODE_ALREADY_USED);
            }

            if (lastToken.ExpiresUtc < nowUtc)
            {
                return FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                    EmailVerificationFaultKeys.MSG_CODE_EXPIRED_OR_MISSING,
                    EmailVerificationFaultKeys.FALLBACK_CODE_EXPIRED_OR_MISSING);
            }

            return FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaultKeys.MSG_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaultKeys.FALLBACK_CODE_INVALID_OR_EXPIRED);
        }

        private void ConsumeTokenOrThrow(Guid tokenId)
        {
            int consumedRows = emailVerificationRepository.ConsumeToken(tokenId);

            if (consumedRows > 0)
            {
                return;
            }

            Logger.WarnFormat(
                "{0}: token already consumed or not found for tokenId '{1}'.",
                LOG_CTX_CONFIRM,
                tokenId);

            throw FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                EmailVerificationFaultKeys.MSG_CODE_ALREADY_USED,
                EmailVerificationFaultKeys.FALLBACK_CODE_ALREADY_USED);
        }

        private void MarkEmailVerifiedOrThrow(long accountId, DateTime nowUtc)
        {
            bool updated = accountRepository.MarkEmailVerified(accountId, nowUtc);

            if (updated)
            {
                return;
            }

            Logger.WarnFormat(
                "{0}: could not mark email as verified for accountId '{1}'.",
                LOG_CTX_CONFIRM,
                accountId);

            throw FaultsFactory.Create(
                EmailVerificationFaultKeys.CODE_EMAIL_VERIFICATION_FAILED,
                EmailVerificationFaultKeys.MSG_EMAIL_VERIFICATION_FAILED,
                EmailVerificationFaultKeys.FALLBACK_EMAIL_VERIFICATION_FAILED);
        }

        private AccountRecord LoadUnverifiedAccountOrSkip(long accountId)
        {
            AccountRecord account = accountRepository.GetAccountByIdAccount(accountId);

            if (account == null || !account.IsValid)
            {
                Logger.WarnFormat(
                    "LoadUnverifiedAccountOrSkip: account not found for accountId '{0}'.",
                    accountId);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_ACCOUNT_NOT_FOUND,
                    EmailVerificationFaultKeys.MSG_ACCOUNT_NOT_FOUND,
                    EmailVerificationFaultKeys.FALLBACK_ACCOUNT_NOT_FOUND);
            }

            if (account.IsEmailVerified)
            {
                Logger.InfoFormat(
                    "LoadUnverifiedAccountOrSkip: email already verified for accountId '{0}'.",
                    accountId);

                return null;
            }

            return account;
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
