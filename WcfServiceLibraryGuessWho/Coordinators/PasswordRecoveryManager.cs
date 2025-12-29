using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
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
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class PasswordRecoveryManager : ManagerBase, IPasswordRecoveryManager
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(PasswordRecoveryManager));

        private const string LOG_CTX_SEND = "PasswordRecoveryManager.SendRecoveryPassword";

        private const string LOG_CTX_UPDATE = "PasswordRecoveryManager.UpdatePasswordWithVerificationCode";

        private const string LOG_CTX_REGEX_TIMEOUT = "PasswordRecoveryManager.RegexTimeout";

        private const int MIN_EXPIRATION_MINUTES = 1;

        private readonly IUserAccountRepository accountRepository;
        private readonly IEmailVerificationRepository emailVerificationRepository;
        private readonly IEmailSender emailSender;
        private readonly IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IEmailVerificationDomainService emailVerificationDomainService;
        private readonly IPasswordHasher passwordHasher;

        public PasswordRecoveryManager(
            IUserAccountRepository accountRepository,
            IEmailVerificationRepository emailVerificationRepository,
            IEmailSender emailSender,
            IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder,
            IVerificationCodeService verificationCodeService,
            IEmailVerificationDomainService emailVerificationDomainService,
            IPasswordHasher passwordHasher)
        {
            this.accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
            this.emailVerificationRepository = emailVerificationRepository ??
                throw new ArgumentNullException(nameof(emailVerificationRepository));
            this.emailSender = emailSender ??
                throw new ArgumentNullException(nameof(emailSender));
            this.verificationCodeEmailBuilder = verificationCodeEmailBuilder ??
                throw new ArgumentNullException(nameof(verificationCodeEmailBuilder));
            this.verificationCodeService = verificationCodeService ??
                throw new ArgumentNullException(nameof(verificationCodeService));
            this.emailVerificationDomainService = emailVerificationDomainService ??
                throw new ArgumentNullException(nameof(emailVerificationDomainService));
            this.passwordHasher = passwordHasher ??
                throw new ArgumentNullException(nameof(passwordHasher));
        }

        public PasswordRecoveryResponse SendRecoveryPassword(PasswordRecoveryRequest request)
        {
            return ExecuteService(
                LOG_CTX_SEND,
                () =>
                {
                    EnsureRecoveryRequestIsNotNull(request);

                    string normalizedEmail = NormalizeEmail(request.Email);
                    DateTime nowUtc = DateTime.UtcNow;

                    if (IsEmailMissing(normalizedEmail))
                    {
                        return CreateAmbiguousSuccessResponse();
                    }

                    Logger.InfoFormat("{0}: requested for email '{1}'.", LOG_CTX_SEND, normalizedEmail);

                    long accountId = GetAccountIdOrAmbiguousSuccess(normalizedEmail);
                    if (accountId <= 0)
                    {
                        return CreateAmbiguousSuccessResponse();
                    }

                    emailVerificationDomainService.ValidateResendLimitsOrThrow(accountId, nowUtc);

                    VerificationCodeResult verificationCodeResult =
                        verificationCodeService.CreateVerificationCodeOrFault();

                    TimeSpan lifeTime = emailVerificationDomainService.GetVerificationCodeLifetime();

                    CreateVerificationTokenOrThrow(accountId, verificationCodeResult, nowUtc, lifeTime);

                    SendRecoveryEmailOrThrow(normalizedEmail, accountId, verificationCodeResult, lifeTime);

                    return CreateSuccessResponse();
                },
                customErrorHandler: HandleRegexTimeout);
        }

        private static bool IsEmailMissing(string normalizedEmail)
        {
            return string.IsNullOrWhiteSpace(normalizedEmail);
        }

        private long GetAccountIdOrAmbiguousSuccess(string normalizedEmail)
        {
            long accountId = accountRepository.GetAccountIdByEmail(normalizedEmail);

            if (accountId > 0)
            {
                return accountId;
            }

            Logger.WarnFormat(
                "{0}: account not found for email '{1}'. Returning ambiguous success.",
                LOG_CTX_SEND,
                normalizedEmail);

            return 0;
        }

        private void CreateVerificationTokenOrThrow(
            long accountId,
            VerificationCodeResult verificationCodeResult,
            DateTime nowUtc,
            TimeSpan lifeTime)
        {
            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = accountId,
                CodeHash = verificationCodeResult.HashCode,
                NowUtc = nowUtc,
                LifeSpan = lifeTime
            };

            bool tokenCreated = emailVerificationRepository.AddVerificationToken(createTokenArgs);

            if (tokenCreated)
            {
                return;
            }

            Logger.WarnFormat("{0}: token creation failed for accountId '{1}'.",
                LOG_CTX_SEND, accountId);

            throw FaultsFactory.Create(
                PasswordRecoveryFaultKeys.CODE_TOKEN_CREATION_FAILED,
                PasswordRecoveryFaultKeys.MSG_TOKEN_CREATION_FAILED,
                PasswordRecoveryFaultKeys.FALLBACK_TOKEN_CREATION_FAILED);
        }

        private void SendRecoveryEmailOrThrow(string normalizedEmail, long accountId, VerificationCodeResult verificationCodeResult, TimeSpan lifeTime)
        {
            int expirationMinutes = Math.Max(MIN_EXPIRATION_MINUTES,
                (int)Math.Ceiling(lifeTime.TotalMinutes));

            var emailContext = new VerificationCodeEmailContext(
                normalizedEmail,
                verificationCodeResult.PlainCode,
                expirationMinutes);

            var message = verificationCodeEmailBuilder.Build(emailContext);

            EmailSendResult sendResult = emailSender.Send(message);

            if (sendResult != null && sendResult.IsSuccess)
            {
                return;
            }

            Logger.WarnFormat("{0}: recovery email send failed for accountId '{1}'. Status='{2}', ErrorCode='{3}'.",
                LOG_CTX_SEND,
                accountId,
                sendResult != null ? sendResult.Status.ToString() : "NULL",
                sendResult != null ? sendResult.ErrorCode : string.Empty);

            if (sendResult != null && sendResult.TechnicalException != null)
            {
                Logger.Error(LOG_CTX_SEND + ": email technical exception.", sendResult.TechnicalException);
            }

            throw EmailFaultTranslator.ToInfrastructureEmailFault(sendResult);
        }

        private static PasswordRecoveryResponse CreateSuccessResponse()
        {
            return new PasswordRecoveryResponse
            {
                Success = true,
                Message = PasswordRecoveryFaultKeys.MSG_RECOVERY_SENT
            };
        }

        public bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request)
        {
            return ExecuteService(
                LOG_CTX_UPDATE,
                () =>
                {
                    EnsureUpdateRequestIsNotNull(request);

                    string normalizedEmail = NormalizeEmail(request.Email);
                    string trimmedCode = (request.VerificationCode ?? string.Empty).Trim();
                    string newPassword = request.NewPassword ?? string.Empty;
                    DateTime nowUtc = DateTime.UtcNow;

                    if (string.IsNullOrWhiteSpace(normalizedEmail))
                    {
                        throw FaultsFactory.Create(
                            PasswordRecoveryFaultKeys.CODE_ACCOUNT_NOT_FOUND,
                            PasswordRecoveryFaultKeys.MSG_ACCOUNT_NOT_FOUND,
                            PasswordRecoveryFaultKeys.FALLBACK_ACCOUNT_NOT_FOUND);
                    }

                    long accountId = accountRepository.GetAccountIdByEmail(normalizedEmail);

                    if (accountId <= 0)
                    {
                        throw FaultsFactory.Create(
                            PasswordRecoveryFaultKeys.CODE_ACCOUNT_NOT_FOUND,
                            PasswordRecoveryFaultKeys.MSG_ACCOUNT_NOT_FOUND,
                            PasswordRecoveryFaultKeys.FALLBACK_ACCOUNT_NOT_FOUND);
                    }

                    EmailVerificationTokenRecord activeToken =
                        emailVerificationRepository.GetLatestActiveTokenByAccountId(accountId, nowUtc);

                    if (activeToken == null || !activeToken.IsValid)
                    {
                        throw FaultsFactory.Create(
                            PasswordRecoveryFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                            PasswordRecoveryFaultKeys.MSG_CODE_EXPIRED,
                            PasswordRecoveryFaultKeys.FALLBACK_CODE_EXPIRED);
                    }

                    var tokenMatchArgs = new ValidateTokenMatchArgs(accountId, trimmedCode, activeToken, nowUtc);

                    emailVerificationDomainService.ValidateTokenMatchOrThrow(tokenMatchArgs);

                    int consumedRows = emailVerificationRepository.ConsumeToken(activeToken.TokenId);

                    if (consumedRows <= 0)
                    {
                        throw FaultsFactory.Create(
                            PasswordRecoveryFaultKeys.CODE_CODE_INVALID_OR_EXPIRED,
                            PasswordRecoveryFaultKeys.MSG_CODE_INVALID_OR_EXPIRED,
                            PasswordRecoveryFaultKeys.FALLBACK_CODE_INVALID_OR_EXPIRED);
                    }

                    byte[] newPasswordHash = passwordHasher.HashPassword(newPassword);

                    var updatePasswordArgs = new UpdatePasswordArgs
                    {
                        AccountId = accountId,
                        NewPasswordHash = newPasswordHash,
                        UpdatedAtUtc = nowUtc
                    };

                    bool passwordUpdated = accountRepository.UpdatePasswordOnly(updatePasswordArgs);

                    if (passwordUpdated)
                    {
                        return true;
                    }

                    throw FaultsFactory.Create(
                        PasswordRecoveryFaultKeys.CODE_UPDATE_PASSWORD_DB_FAILED,
                        PasswordRecoveryFaultKeys.MSG_UPDATE_PASSWORD_DB_FAILED,
                        PasswordRecoveryFaultKeys.FALLBACK_UPDATE_PASSWORD_DB_FAILED);
                },
                customErrorHandler: HandleRegexTimeout);
        }

        private void HandleRegexTimeout(Exception ex)
        {
            if (ex is RegexMatchTimeoutException timeoutEx)
            {
                Logger.Error(LOG_CTX_REGEX_TIMEOUT, timeoutEx);

                throw FaultsFactory.Create(
                    PasswordRecoveryFaultKeys.CODE_UNEXPECTED_ERROR,
                    PasswordRecoveryFaultKeys.MSG_UNEXPECTED_ERROR,
                    PasswordRecoveryFaultKeys.FALLBACK_UNEXPECTED_ERROR,
                    timeoutEx);
            }
        }

        private static void EnsureRecoveryRequestIsNotNull(PasswordRecoveryRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                PasswordRecoveryFaultKeys.CODE_REQUEST_NULL,
                PasswordRecoveryFaultKeys.MSG_REQUEST_NULL,
                PasswordRecoveryFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static void EnsureUpdateRequestIsNotNull(UpdatePasswordRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                PasswordRecoveryFaultKeys.CODE_REQUEST_NULL,
                PasswordRecoveryFaultKeys.MSG_REQUEST_NULL,
                PasswordRecoveryFaultKeys.FALLBACK_REQUEST_NULL);
        }

        private static PasswordRecoveryResponse CreateAmbiguousSuccessResponse()
        {
            return new PasswordRecoveryResponse
            {
                Success = true,
                Message = PasswordRecoveryFaultKeys.FALLBACK_AMBIGUOUS_SUCCESS
            };
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
