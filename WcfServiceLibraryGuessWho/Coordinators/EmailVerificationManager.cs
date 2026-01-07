using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Parameters.EmailVerification;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Communication.Email.Builders.Context;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoServices.Coordinators
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

        private const string LOG_MSG_ACCOUNT_NOT_FOUND =
            "{0}: account not found for accountId '{1}'.";

        private const string LOG_MSG_ALREADY_VERIFIED_SKIP =
            "{0}: email already verified for accountId '{1}'. Skipping.";

        private const string LOG_MSG_TOKEN_CREATION_FAILED =
            "{0}: token creation failed for accountId '{1}'.";

        private const string LOG_MSG_TOKEN_ALREADY_CONSUMED =
            "{0}: token already consumed or not found for tokenId '{1}'.";

        private const string LOG_MSG_MARK_VERIFIED_FAILED =
            "{0}: could not mark email as verified for accountId '{1}'.";

        private const string LOG_MSG_EMAIL_MESSAGE_BUILD_RETURNED_NULL =
            "{0}: email builder returned null message for accountId '{1}'.";

        private const string LOG_MSG_EMAIL_SENDER_RETURNED_NULL =
            "{0}: email sender returned null result for accountId '{1}'.";

        private const string LOG_MSG_EMAIL_SEND_FAILED =
            "{0}: email send failed for accountId '{1}'. Status='{2}', Code='{3}'.";

        private const string LOG_MSG_EMAIL_TECHNICAL_EXCEPTION =
            "{0}: email technical exception.";

        private const int MIN_EXPIRATION_MINUTES = 1;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory; 
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IEmailSender emailSender;
        private readonly VerificationCodeEmailBuilder verificationCodeEmailBuilder;
        private readonly IEmailVerificationDomainService domainService;

        public EmailVerificationManager(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory, 
            IVerificationCodeService verificationCodeService,
            IEmailSender emailSender,
            VerificationCodeEmailBuilder verificationCodeEmailBuilder,
            IEmailVerificationDomainService domainService)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
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

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create()) 
                    using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction()) 
                    {
                        AccountLookupResult accountLookup = LoadUnverifiedAccountOrSkip(unitOfWork, request.AccountId);

                        if (accountLookup.ShouldSkip)
                        {
                            transaction.Commit();
                            return new VerifyEmailResponse { Success = true };
                        }

                        EmailVerificationTokenRecord activeToken =
                            unitOfWork.EmailVerification.GetLatestActiveTokenByAccountId(request.AccountId, nowUtc);

                        if (activeToken == null || !activeToken.IsValid)
                        {
                            throw CreateInvalidOrExpiredVerificationCodeFault(unitOfWork, request.AccountId, nowUtc);
                        }

                        var matchArgs = new ValidateTokenMatchArgs(
                            request.AccountId,
                            trimmedCode,
                            activeToken,
                            nowUtc);

                        domainService.ValidateTokenMatchOrThrow(matchArgs);

                        ConsumeTokenOrThrow(unitOfWork, activeToken.TokenId);
                        MarkEmailVerifiedOrThrow(unitOfWork, request.AccountId, nowUtc);

                        unitOfWork.Flush(); 
                        transaction.Commit();

                        return new VerifyEmailResponse { Success = true };
                    }
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

                    AccountRecord account;
                    VerificationCodeResult verificationCode;
                    int expirationMinutes;

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create()) 
                    {
                        AccountLookupResult accountLookup = LoadUnverifiedAccountOrSkip(unitOfWork, request.AccountId);

                        if (accountLookup.ShouldSkip)
                        {
                            return;
                        }

                        account = accountLookup.Account;

                        domainService.ValidateResendLimitsOrThrow(request.AccountId, nowUtc);

                        verificationCode = verificationCodeService.CreateVerificationCodeOrFault();

                        TimeSpan lifeTime = domainService.GetVerificationCodeLifetime();

                        var createTokenArgs = new CreateEmailTokenArgs
                        {
                            AccountId = request.AccountId,
                            CodeHash = verificationCode.HashCode,
                            NowUtc = nowUtc,
                            LifeSpan = lifeTime
                        };

                        bool created = unitOfWork.EmailVerification.AddVerificationToken(createTokenArgs);

                        if (!created)
                        {
                            Logger.WarnFormat(LOG_MSG_TOKEN_CREATION_FAILED, LOG_CTX_RESEND, request.AccountId);

                            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_TOKEN_CREATION_FAILED);
                        }

                        unitOfWork.Flush(); 

                        expirationMinutes = Math.Max(
                            MIN_EXPIRATION_MINUTES,
                            (int)Math.Ceiling(lifeTime.TotalMinutes));
                    }

                    SendVerificationEmailOrThrow(
                        account.Email,
                        request.AccountId,
                        verificationCode.PlainCode,
                        expirationMinutes);
                },
                customErrorHandler: HandleRegexTimeout);
        }

        private void SendVerificationEmailOrThrow(string email, long accountId, string plainCode, int expirationMinutes)
        {
            EmailMessage message = verificationCodeEmailBuilder.Build(
                new VerificationCodeEmailContext(
                    email,
                    plainCode ?? string.Empty,
                    expirationMinutes));

            if (message == null)
            {
                Logger.WarnFormat(LOG_MSG_EMAIL_MESSAGE_BUILD_RETURNED_NULL, LOG_CTX_RESEND, accountId);

                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_UNEXPECTED_ERROR);
            }

            EmailSendResult sendResult = emailSender.Send(message);

            if (sendResult == null)
            {
                Logger.WarnFormat(LOG_MSG_EMAIL_SENDER_RETURNED_NULL, LOG_CTX_RESEND, accountId);

                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_UNEXPECTED_ERROR);
            }

            if (sendResult.IsSuccess)
            {
                return;
            }

            Logger.WarnFormat(LOG_MSG_EMAIL_SEND_FAILED, LOG_CTX_RESEND, accountId, sendResult.Status, sendResult.ErrorCode);

            if (sendResult.TechnicalException != null)
            {
                Logger.ErrorFormat(LOG_MSG_EMAIL_TECHNICAL_EXCEPTION, LOG_CTX_RESEND, sendResult.TechnicalException);
            }

            throw EmailFaultTranslator.ToInfrastructureEmailFault(sendResult);
        }

        private void HandleRegexTimeout(Exception ex)
        {
            if (ex is RegexMatchTimeoutException timeoutEx)
            {
                Logger.Error(LOG_CTX_REGEX_TIMEOUT, timeoutEx);

                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_UNEXPECTED_ERROR, timeoutEx);
            }
        }

        private static void EnsureRequestIsNotNull(VerifyEmailRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_REQUEST_NULL);
        }

        private static void EnsureResendRequestIsNotNull(ResendVerificationRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_REQUEST_NULL);
        }

        private FaultException<ServiceFault> CreateInvalidOrExpiredVerificationCodeFault(
            IGuessWhoUnitOfWork unitOfWork, 
            long accountId,
            DateTime nowUtc)
        {
            EmailVerificationTokenRecord lastToken =
                unitOfWork.EmailVerification.GetLatestTokenStatusByAccountId(accountId);

            if (lastToken == null)
            {
                return FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_MISSING);
            }

            if (lastToken.ConsumedUtc.HasValue)
            {
                return FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_ALREADY_USED);
            }

            if (lastToken.ExpiresUtc < nowUtc)
            {
                return FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_EXPIRED);
            }

            return FaultsFactory.Create(EmailVerificationFaultKeys.CODE_UNEXPECTED_ERROR);
        }

        private void ConsumeTokenOrThrow(IGuessWhoUnitOfWork unitOfWork, Guid tokenId)
        {
            int consumedRows = unitOfWork.EmailVerification.ConsumeToken(tokenId);

            if (consumedRows > 0)
            {
                return;
            }

            Logger.WarnFormat(LOG_MSG_TOKEN_ALREADY_CONSUMED, LOG_CTX_CONFIRM, tokenId);

            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_CODE_ALREADY_USED);
        }

        private void MarkEmailVerifiedOrThrow(IGuessWhoUnitOfWork unitOfWork, long accountId, DateTime nowUtc)
        {
            bool updated = unitOfWork.UserAccounts.MarkEmailVerified(accountId, nowUtc);

            if (updated)
            {
                return;
            }

            Logger.WarnFormat(LOG_MSG_MARK_VERIFIED_FAILED, LOG_CTX_CONFIRM, accountId);

            throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_EMAIL_VERIFICATION_FAILED);
        }

        private AccountLookupResult LoadUnverifiedAccountOrSkip(IGuessWhoUnitOfWork unitOfWork, long accountId)
        {
            AccountRecord account = unitOfWork.UserAccounts.GetAccountByIdAccount(accountId);

            if (account == null || !account.IsValid)
            {
                Logger.WarnFormat(LOG_MSG_ACCOUNT_NOT_FOUND, LOG_CTX_CONFIRM, accountId);

                throw FaultsFactory.Create(EmailVerificationFaultKeys.CODE_ACCOUNT_NOT_FOUND);
            }

            if (account.IsEmailVerified)
            {
                Logger.InfoFormat(LOG_MSG_ALREADY_VERIFIED_SKIP, LOG_CTX_CONFIRM, accountId);

                return AccountLookupResult.Skip();
            }

            return AccountLookupResult.Found(account);
        }

        private sealed class AccountLookupResult
        {
            private AccountLookupResult(bool shouldSkip, AccountRecord account)
            {
                ShouldSkip = shouldSkip;
                Account = account;
            }

            public bool ShouldSkip { get; }
            public AccountRecord Account { get; }

            public static AccountLookupResult Skip()
            {
                return new AccountLookupResult(shouldSkip: true, account: null);
            }

            public static AccountLookupResult Found(AccountRecord account)
            {
                if (account == null)
                {
                    throw new ArgumentNullException(nameof(account));
                }

                return new AccountLookupResult(shouldSkip: false, account: account);
            }
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
