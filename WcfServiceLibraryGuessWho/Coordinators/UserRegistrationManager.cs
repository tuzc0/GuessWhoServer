using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Validation;
using GuessWhoCore.Validation.ValidationDTOs;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServerDomain.Domain.Results.Accounts;
using GuessWhoServices.Coordinators.InternalDtos;
using GuessWhoServices.Errors;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using GuessWhoServices.Communication.Email;
using GuessWhoServices.Communication.Email.Builders;
using GuessWhoServices.Communication.Email.Builders.Context;
using GuessWhoServices.Coordinators.Base;
using GuessWhoServices.Coordinators.Interfaces;
using GuessWhoDataAccess.Data.Factories;

namespace GuessWhoServices.Coordinators
{
    public sealed class RegisterResultArgs
    {
        public RegisterResultArgs(long accountId, long userId, string email)
        {
            AccountId = accountId;
            UserId = userId;
            Email = email ?? string.Empty;
        }

        public long AccountId { get; }
        public long UserId { get; }
        public string Email { get; }

        public string DisplayName { get; set; } = string.Empty;
        public bool EmailVerificationRequired { get; set; }
    }

    public sealed class RegisterResult
    {
        public RegisterResult(RegisterResultArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            AccountId = args.AccountId;
            UserId = args.UserId;
            Email = args.Email ?? string.Empty;
            DisplayName = args.DisplayName ?? string.Empty;
            EmailVerificationRequired = args.EmailVerificationRequired;
        }

        public long AccountId { get; }
        public long UserId { get; }
        public string Email { get; }
        public string DisplayName { get; }
        public bool EmailVerificationRequired { get; }
    }

    public sealed class UserRegistrationManager : ManagerBase, IUserRegistrationManager
    {
        protected override ILog Logger { get; } =
            LogManager.GetLogger(typeof(UserRegistrationManager));

        private const string LOG_CTX_REGISTER = "UserRegistrationManager.RegisterUser";

        private const int MIN_EXPIRATION_MINUTES = 1;

        private const bool EMAIL_VERIFICATION_REQUIRED = true;

        private const string EMPTY = "";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IEmailSender emailSender;
        private readonly IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder;
        private readonly IPasswordHasher passwordHasher;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly TimeSpan verificationCodeLifeTime;

        public UserRegistrationManager(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IEmailSender emailSender,
            IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder,
            IPasswordHasher passwordHasher,
            IVerificationCodeService verificationCodeService,
            TimeSpan verificationCodeLifeTime)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.emailSender = emailSender ??
                throw new ArgumentNullException(nameof(emailSender));
            this.verificationCodeEmailBuilder = verificationCodeEmailBuilder ??
                throw new ArgumentNullException(nameof(verificationCodeEmailBuilder));
            this.passwordHasher = passwordHasher ??
                throw new ArgumentNullException(nameof(passwordHasher));
            this.verificationCodeService = verificationCodeService ??
                throw new ArgumentNullException(nameof(verificationCodeService));

            if (verificationCodeLifeTime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(verificationCodeLifeTime));
            }

            this.verificationCodeLifeTime = verificationCodeLifeTime;
        }

        public RegisterResult RegisterUser(RegisterUserArgs registerUserArgs)
        {
            return ExecuteService(
                LOG_CTX_REGISTER,
                () =>
                {
                    NormalizedRegistration normalized = ValidateAndNormalize(registerUserArgs);

                    RegistrationDbResult dbResult;

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    {
                        dbResult = CreateAccountAndToken(unitOfWork, normalized);
                    }

                    int expirationMinutes = GetExpirationMinutes();

                    TrySendVerificationEmail(new SendVerificationEmailArgs(
                        normalized.Email,
                        dbResult.VerificationCode.PlainCode,
                        dbResult.Account.AccountId,
                        expirationMinutes));

                    var resultArgs = new RegisterResultArgs(
                        dbResult.Account.AccountId,
                        dbResult.Profile.UserId,
                        normalized.Email)
                    {
                        DisplayName = dbResult.Profile.DisplayName,
                        EmailVerificationRequired = EMAIL_VERIFICATION_REQUIRED
                    };

                    return new RegisterResult(resultArgs);
                });
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }

        private NormalizedRegistration ValidateAndNormalize(RegisterUserArgs userArgs)
        {
            EnsureArgsNotNull(userArgs);

            DateTime nowUtc = EnsureNowUtcIsValid(userArgs.NowUtc);

            var passwords = new PasswordConfirmationDraft(
                userArgs.Password,
                userArgs.Password);

            var draft = new UserRulesDraft(
                userArgs.Email,
                userArgs.DisplayName,
                passwords);

            IReadOnlyList<ValidationError> errors = UserRules.Validate(draft);

            if (errors != null && errors.Count > 0)
            {
                ThrowValidationFault(errors[0]);
            }

            return new NormalizedRegistration(userArgs, nowUtc);
        }

        private static void EnsureArgsNotNull(RegisterUserArgs registerUserArgs)
        {
            if (registerUserArgs != null)
            {
                return;
            }

            throw FaultsFactory.Create(
                UserRegistrationFaultKeys.CODE_ARGS_REQUIRED,
                UserRegistrationFaultKeys.MSG_ARGS_REQUIRED,
                UserRegistrationFaultKeys.FALLBACK_ARGS_REQUIRED);
        }

        private static DateTime EnsureNowUtcIsValid(DateTime nowUtc)
        {
            if (nowUtc != default)
            {
                return nowUtc;
            }

            throw FaultsFactory.Create(
                UserRegistrationFaultKeys.CODE_NOWUTC_REQUIRED,
                UserRegistrationFaultKeys.MSG_NOWUTC_REQUIRED,
                UserRegistrationFaultKeys.FALLBACK_NOWUTC_REQUIRED);
        }

        private void ThrowValidationFault(ValidationError error)
        {
            string key = error != null ? error.Key ?? EMPTY : EMPTY;

            switch (key)
            {
                case "Registration.InvalidRequest":

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_ARGS_REQUIRED,
                        UserRegistrationFaultKeys.MSG_ARGS_REQUIRED,
                        UserRegistrationFaultKeys.FALLBACK_ARGS_REQUIRED);

                case "Registration.Email.Required":

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_EMAIL_REQUIRED,
                        UserRegistrationFaultKeys.MSG_EMAIL_REQUIRED,
                        UserRegistrationFaultKeys.FALLBACK_EMAIL_REQUIRED);

                case "Registration.Email.TooLong":
                case "Registration.Email.InvalidFormat":

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_EMAIL_INVALID,
                        UserRegistrationFaultKeys.MSG_EMAIL_INVALID,
                        UserRegistrationFaultKeys.FALLBACK_EMAIL_INVALID);

                case "Registration.DisplayName.Required":
                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_DISPLAYNAME_REQUIRED,
                        UserRegistrationFaultKeys.MSG_DISPLAYNAME_REQUIRED,
                        UserRegistrationFaultKeys.FALLBACK_DISPLAYNAME_REQUIRED);

                case "Registration.DisplayName.TooShort":
                case "Registration.DisplayName.TooLong":
                case "Registration.DisplayName.InvalidFormat":

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_DISPLAYNAME_INVALID,
                        UserRegistrationFaultKeys.MSG_DISPLAYNAME_INVALID,
                        UserRegistrationFaultKeys.FALLBACK_DISPLAYNAME_INVALID);

                case "Registration.Password.Required":
                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_PASSWORD_REQUIRED,
                        UserRegistrationFaultKeys.MSG_PASSWORD_REQUIRED,
                        UserRegistrationFaultKeys.FALLBACK_PASSWORD_REQUIRED);

                case "Registration.Password.TooShort":
                case "Registration.Password.TooLong":
                case "Registration.ConfirmPassword.Required":
                case "Registration.ConfirmPassword.Mismatch":
                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_PASSWORD_INVALID,
                        UserRegistrationFaultKeys.MSG_PASSWORD_INVALID,
                        UserRegistrationFaultKeys.FALLBACK_PASSWORD_INVALID);

                default:
                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_UNEXPECTED_ERROR,
                        UserRegistrationFaultKeys.MSG_UNEXPECTED_ERROR,
                        UserRegistrationFaultKeys.FALLBACK_UNEXPECTED_ERROR);
            }
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? EMPTY).Trim().ToLowerInvariant();
        }

        private int GetExpirationMinutes()
        {
            return Math.Max(MIN_EXPIRATION_MINUTES, (int)Math.Ceiling(verificationCodeLifeTime.TotalMinutes));
        }

        private RegistrationDbResult CreateAccountAndToken(IGuessWhoUnitOfWork unitOfWork, NormalizedRegistration registration)
        {
            if (unitOfWork == null)
            {
                throw new ArgumentNullException(nameof(unitOfWork));
            }

            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            byte[] passwordHash = passwordHasher.HashPassword(registration.Password);
            VerificationCodeResult codeResult = verificationCodeService.CreateVerificationCodeOrFault();

            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                string defaultAvatarId = unitOfWork.Avatars.GetDefaultAvatarId();

                if (string.IsNullOrWhiteSpace(defaultAvatarId))
                {
                    Logger.ErrorFormat("{0}: default avatar id not configured.", LOG_CTX_REGISTER);

                    throw FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_DEFAULT_AVATAR_NOT_CONFIGURED,
                        InfrastructureFaultKeys.MSG_DEFAULT_AVATAR_NOT_CONFIGURED,
                        InfrastructureFaultKeys.FALLBACK_DEFAULT_AVATAR_NOT_CONFIGURED);
                }

                if (unitOfWork.UserAccounts.EmailExists(registration.Email))
                {
                    Logger.WarnFormat("{0}: email already exists '{1}'.", LOG_CTX_REGISTER, registration.Email);

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_EMAIL_ALREADY_EXISTS,
                        UserRegistrationFaultKeys.MSG_EMAIL_ALREADY_EXISTS,
                        UserRegistrationFaultKeys.FALLBACK_EMAIL_ALREADY_EXISTS);
                }

                var createAccountArgs = new CreateAccountArgs
                {
                    Email = registration.Email,
                    PasswordHash = passwordHash,
                    DisplayName = registration.DisplayName,
                    CreationDate = registration.NowUtc,
                    AvatarId = defaultAvatarId
                };

                CreatedAccountResult created = unitOfWork.UserAccounts.CreateAccount(createAccountArgs);

                var tokenArgs = new CreateEmailTokenArgs
                {
                    AccountId = created.Account.AccountId,
                    CodeHash = codeResult.HashCode,
                    NowUtc = registration.NowUtc,
                    LifeSpan = verificationCodeLifeTime
                };

                bool tokenCreated = unitOfWork.EmailVerification.AddVerificationToken(tokenArgs);

                if (!tokenCreated)
                {
                    Logger.WarnFormat("{0}: token creation failed for accountId '{1}'.", LOG_CTX_REGISTER, created.Account.AccountId);

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_TOKEN_CREATION_FAILED,
                        UserRegistrationFaultKeys.MSG_TOKEN_CREATION_FAILED,
                        UserRegistrationFaultKeys.FALLBACK_TOKEN_CREATION_FAILED);
                }

                unitOfWork.Flush();
                transaction.Commit();

                return new RegistrationDbResult(created.Account, created.Profile, codeResult);
            }
        }

        private readonly record struct SendVerificationEmailArgs(
            string Email,
            string Code,
            long AccountId,
            int ExpirationMinutes);

        private void TrySendVerificationEmail(SendVerificationEmailArgs args)
        {
            var context = new VerificationCodeEmailContext(
                args.Email ?? EMPTY,
                args.Code ?? EMPTY,
                args.ExpirationMinutes);

            EmailMessage message = verificationCodeEmailBuilder.Build(context);

            if (message == null)
            {
                Logger.WarnFormat("{0}: verification email builder returned null message for accountId '{1}'.", 
                    LOG_CTX_REGISTER, args.AccountId);
                return;
            }

            EmailSendResult sendResult = emailSender.Send(message);

            if (sendResult == null)
            {
                Logger.WarnFormat("{0}: email sender returned null result for accountId '{1}'.", 
                    LOG_CTX_REGISTER, args.AccountId);
                return;
            }

            if (sendResult.IsSuccess)
            {
                return;
            }

            if (sendResult.TechnicalException != null)
            {
                Logger.ErrorFormat("{0}: email technical exception.", LOG_CTX_REGISTER, sendResult.TechnicalException);
            }
        }

        private sealed class NormalizedRegistration
        {
            public NormalizedRegistration(RegisterUserArgs userArgs, DateTime nowUtc)
            {
                Email = NormalizeEmail(userArgs != null ? userArgs.Email : EMPTY);
                DisplayName = (userArgs != null ? userArgs.DisplayName ?? EMPTY : EMPTY).Trim();
                Password = userArgs != null ? userArgs.Password ?? EMPTY : EMPTY;
                NowUtc = nowUtc;
            }

            public string Email { get; }
            public string DisplayName { get; }
            public string Password { get; }
            public DateTime NowUtc { get; }
        }

        private sealed class RegistrationDbResult
        {
            public RegistrationDbResult(AccountRecord account, UserProfileRecord profile, 
                VerificationCodeResult verificationCode)
            {
                Account = account ?? 
                    throw new ArgumentNullException(nameof(account));
                Profile = profile ?? 
                    throw new ArgumentNullException(nameof(profile));
                VerificationCode = verificationCode ?? 
                    throw new ArgumentNullException(nameof(verificationCode));
            }

            public AccountRecord Account { get; }
            public UserProfileRecord Profile { get; }
            public VerificationCodeResult VerificationCode { get; }
        }
    }
}
