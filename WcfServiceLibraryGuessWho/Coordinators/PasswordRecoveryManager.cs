using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.EmailVerification;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
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
    public sealed class PasswordRecoveryManager : ManagerBase, IPasswordRecoveryManager
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(PasswordRecoveryManager));

        private const string LOG_CTX_SEND = "PasswordRecoveryManager.SendRecoveryPassword";
        private const string LOG_CTX_UPDATE = "PasswordRecoveryManager.UpdatePasswordWithVerificationCode";
        private const string LOG_CTX_REGEX_TIMEOUT = "PasswordRecoveryManager.RegexTimeout";

        private const string LOG_MSG_RECOVERY_REQUESTED =
            "{0}: requested for email '{1}'.";

        private const string LOG_MSG_ACCOUNT_NOT_FOUND_AMBIGUOUS =
            "{0}: account not found for email '{1}'. Returning ambiguous success.";

        private const string LOG_MSG_TOKEN_CREATION_FAILED =
            "{0}: token creation failed for accountId '{1}'.";

        private const string LOG_MSG_EMAIL_MESSAGE_BUILD_RETURNED_NULL =
            "{0}: recovery email builder returned null message for accountId '{1}'.";

        private const string LOG_MSG_EMAIL_SENDER_RETURNED_NULL =
            "{0}: email sender returned null result for accountId '{1}'.";

        private const string LOG_MSG_EMAIL_SEND_FAILED =
            "{0}: recovery email send failed for accountId '{1}'. Status='{2}', ErrorCode='{3}'.";

        private const string LOG_MSG_EMAIL_TECHNICAL_EXCEPTION =
            "{0}: email technical exception.";

        private const int MIN_EXPIRATION_MINUTES = 1;

        private const long INVALID_ACCOUNT_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IEmailSender emailSender;
        private readonly IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly IEmailVerificationDomainService emailVerificationDomainService;
        private readonly IPasswordHasher passwordHasher;

        public PasswordRecoveryManager(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IEmailSender emailSender,
            IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder,
            IVerificationCodeService verificationCodeService,
            IEmailVerificationDomainService emailVerificationDomainService,
            IPasswordHasher passwordHasher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
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
                    if (IsEmailMissing(normalizedEmail))
                    {
                        return CreateAmbiguousSuccessResponse();
                    }

                    DateTime nowUtc = DateTime.UtcNow;

                    Logger.InfoFormat(LOG_MSG_RECOVERY_REQUESTED, LOG_CTX_SEND, normalizedEmail);

                    VerificationCodeResult codeResult;
                    TimeSpan lifeTime;
                    long accountId;

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    {
                        accountId = TryGetAccountIdOrZero(unitOfWork, normalizedEmail);
                        if (accountId <= 0)
                        {
                            return CreateAmbiguousSuccessResponse();
                        }

                        emailVerificationDomainService.ValidateResendLimitsOrThrow(accountId, nowUtc);

                        codeResult = verificationCodeService.CreateVerificationCodeOrFault();
                        lifeTime = emailVerificationDomainService.GetVerificationCodeLifetime();

                        PersistRecoveryTokenOrThrow(
                            unitOfWork,
                            new PersistRecoveryToken(
                                accountId,
                                codeResult.HashCode,
                                nowUtc,
                                lifeTime));

                        unitOfWork.Flush();
                        
                    }

                    SendRecoveryEmailOrThrow(
                        new SendRecoveryEmail(
                            normalizedEmail,
                            accountId,
                            codeResult.PlainCode,
                            lifeTime));

                    return CreateSuccessResponse();
                },
                customErrorHandler: HandleRegexTimeout);
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
                        throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_ACCOUNT_NOT_FOUND);
                    }

                    using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
                    using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

                    long accountId = unitOfWork.UserAccounts.GetAccountIdByEmail(normalizedEmail);

                    if (accountId <= 0)
                    {
                        throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_ACCOUNT_NOT_FOUND);
                    }

                    EmailVerificationTokenRecord activeToken =
                        unitOfWork.EmailVerification.GetLatestActiveTokenByAccountId(accountId, nowUtc);

                    if (activeToken == null || !activeToken.IsValid)
                    {
                        throw FaultsFactory.Create(
                            PasswordRecoveryFaultKeys.CODE_CODE_EXPIRED);
                    }

                    var tokenMatchArgs = new ValidateTokenMatchArgs(accountId, trimmedCode, activeToken, nowUtc);

                    emailVerificationDomainService.ValidateTokenMatchOrThrow(tokenMatchArgs);

                    int consumedRows = unitOfWork.EmailVerification.ConsumeToken(activeToken.TokenId);

                    if (consumedRows <= 0)
                    {
                        throw FaultsFactory.Create(
                            PasswordRecoveryFaultKeys.CODE_CODE_INVALID);
                    }

                    byte[] newPasswordHash = passwordHasher.HashPassword(newPassword);

                    var updatePasswordArgs = new UpdatePasswordArgs
                    {
                        AccountId = accountId,
                        NewPasswordHash = newPasswordHash,
                        UpdatedAtUtc = nowUtc
                    };

                    bool passwordUpdated = unitOfWork.UserAccounts.UpdatePasswordOnly(updatePasswordArgs);

                    if (!passwordUpdated)
                    {
                        throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_UPDATE_PASSWORD_DB_FAILED);
                    }

                    unitOfWork.Flush();
                    transaction.Commit();

                    return true;
                },
                customErrorHandler: HandleRegexTimeout);
        }

        private static bool IsEmailMissing(string normalizedEmail)
        {
            return string.IsNullOrWhiteSpace(normalizedEmail);
        }

        private long TryGetAccountIdOrZero(IGuessWhoUnitOfWork unitOfWork, string normalizedEmail)
        {
            if (unitOfWork == null)
            {
                throw new ArgumentNullException(nameof(unitOfWork));
            }

            long accountId = unitOfWork.UserAccounts.GetAccountIdByEmail(normalizedEmail);

            if (accountId > 0)
            {
                return accountId;
            }

            Logger.WarnFormat(LOG_MSG_ACCOUNT_NOT_FOUND_AMBIGUOUS, LOG_CTX_SEND, normalizedEmail);

            return INVALID_ACCOUNT_ID;
        }

        private readonly record struct PersistRecoveryToken(
            long AccountId,
            byte[] CodeHash,
            DateTime NowUtc,
            TimeSpan LifeTime);

        private void PersistRecoveryTokenOrThrow(IGuessWhoUnitOfWork unitOfWork, PersistRecoveryToken persistRecoveryToken)
        {
            if (unitOfWork == null)
            {
                throw new ArgumentNullException(nameof(unitOfWork));
            }

            var createTokenArgs = new CreateEmailTokenArgs
            {
                AccountId = persistRecoveryToken.AccountId,
                CodeHash = persistRecoveryToken.CodeHash ?? Array.Empty<byte>(),
                NowUtc = persistRecoveryToken.NowUtc,
                LifeSpan = persistRecoveryToken.LifeTime
            };

            bool tokenCreated = unitOfWork.EmailVerification.AddVerificationToken(createTokenArgs);

            if (tokenCreated)
            {
                return;
            }

            Logger.WarnFormat(LOG_MSG_TOKEN_CREATION_FAILED, LOG_CTX_SEND, persistRecoveryToken.AccountId);

            throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_TOKEN_CREATION_FAILED);
        }

        private readonly record struct SendRecoveryEmail(
            string Email,
            long AccountId,
            string PlainCode,
            TimeSpan LifeTime);

        private void SendRecoveryEmailOrThrow(SendRecoveryEmail sendRecovery)
        {
            int expirationMinutes = Math.Max(
                MIN_EXPIRATION_MINUTES,
                (int)Math.Ceiling(sendRecovery.LifeTime.TotalMinutes));

            var emailContext = new VerificationCodeEmailContext(
                sendRecovery.Email,
                sendRecovery.PlainCode ?? string.Empty,
                expirationMinutes);

            EmailMessage message = verificationCodeEmailBuilder.Build(emailContext);

            if (message == null)
            {
                Logger.WarnFormat(LOG_MSG_EMAIL_MESSAGE_BUILD_RETURNED_NULL, LOG_CTX_SEND, sendRecovery.AccountId);

                throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_EMAIL_MESSAGE_BUILD_FAILED);
            }

            EmailSendResult sendResult = emailSender.Send(message);

            if (sendResult == null)
            {
                Logger.WarnFormat(LOG_MSG_EMAIL_SENDER_RETURNED_NULL, LOG_CTX_SEND, sendRecovery.AccountId);

                throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_EMAIL_SENDER_RETURNED_NULL);
            }

            if (sendResult.IsSuccess)
            {
                return;
            }

            Logger.WarnFormat(LOG_MSG_EMAIL_SEND_FAILED, LOG_CTX_SEND, sendRecovery.AccountId, sendResult.Status,
                sendResult.ErrorCode);

            if (sendResult.TechnicalException != null)
            {
                Logger.ErrorFormat(LOG_MSG_EMAIL_TECHNICAL_EXCEPTION, LOG_CTX_SEND, sendResult.TechnicalException);
            }

            throw EmailFaultTranslator.ToInfrastructureEmailFault(sendResult);
        }

        private static PasswordRecoveryResponse CreateSuccessResponse()
        {
            return new PasswordRecoveryResponse
            {
                Success = true,
                MessageCode = PasswordRecoveryNoticeCodes.RECOVERY_SENT,
                Message = string.Empty
            };
        }

        private void HandleRegexTimeout(Exception ex)
        {
            if (ex is RegexMatchTimeoutException timeoutEx)
            {
                Logger.Error(LOG_CTX_REGEX_TIMEOUT, timeoutEx);

                throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_UNEXPECTED_ERROR);
            }
        }

        private static void EnsureRecoveryRequestIsNotNull(PasswordRecoveryRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_REQUEST_NULL);
        }

        private static void EnsureUpdateRequestIsNotNull(UpdatePasswordRequest request)
        {
            if (request != null)
            {
                return;
            }

            throw FaultsFactory.Create(PasswordRecoveryFaultKeys.CODE_REQUEST_NULL);
        }

        private static PasswordRecoveryResponse CreateAmbiguousSuccessResponse()
        {
            return new PasswordRecoveryResponse
            {
                Success = true,
                MessageCode = PasswordRecoveryNoticeCodes.AMBIGUOUS_SUCCESS,
                Message = string.Empty
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
