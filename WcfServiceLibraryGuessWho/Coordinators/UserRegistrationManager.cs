using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Faults;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts.Email;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Communication.Email.Builders;
using WcfServiceLibraryGuessWho.Communication.Email.Builders.Context;
using WcfServiceLibraryGuessWho.Coordinators.Base;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.InternalDtos;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class RegisterResult
    {
        public RegisterResult(
            long accountId,
            long userId,
            string email,
            string displayName,
            bool emailVerificationRequired)
        {
            AccountId = accountId;
            UserId = userId;
            Email = email ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            EmailVerificationRequired = emailVerificationRequired;
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

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IAvatarRepository avatarRepository;
        private readonly IEmailSender emailSender;
        private readonly IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder;
        private readonly IPasswordHasher passwordHasher;
        private readonly IVerificationCodeService verificationCodeService;
        private readonly TimeSpan verificationCodeLifeTime;

        public UserRegistrationManager(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IAvatarRepository avatarRepository,
            IEmailSender emailSender,
            IEmailMessageBuilder<VerificationCodeEmailContext> verificationCodeEmailBuilder,
            IPasswordHasher passwordHasher,
            IVerificationCodeService verificationCodeService,
            TimeSpan verificationCodeLifeTime)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.avatarRepository = avatarRepository ?? 
                throw new ArgumentNullException(nameof(avatarRepository));
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
                    NormalizedRegistration userNormalized = ValidateAndNormalize(registerUserArgs);

                    RegistrationDbResult resultDb = CreateAccountAndToken(userNormalized);

                    int expirationMinutes = Math.Max(MIN_EXPIRATION_MINUTES, (int)Math.Ceiling(verificationCodeLifeTime.TotalMinutes));

                    TrySendVerificationEmail(userNormalized.Email, resultDb.VerificationCode.PlainCode, resultDb.Account.AccountId, expirationMinutes);

                    return new RegisterResult(
                        resultDb.Account.AccountId,
                        resultDb.Profile.UserId,
                        userNormalized.Email,
                        resultDb.Profile.DisplayName,
                        emailVerificationRequired: true);
                });
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

        private static string EnsureEmailIsProvided(string email)
        {
            string normalizedEmail = NormalizeEmail(email);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return normalizedEmail;
            }

            throw FaultsFactory.Create(
                UserRegistrationFaultKeys.CODE_EMAIL_REQUIRED,
                UserRegistrationFaultKeys.MSG_EMAIL_REQUIRED,
                UserRegistrationFaultKeys.FALLBACK_EMAIL_REQUIRED);
        }

        private static string EnsurePasswordIsProvided(string password)
        {
            string safePassword = password ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(safePassword))
            {
                return safePassword;
            }

            throw FaultsFactory.Create(
                UserRegistrationFaultKeys.CODE_PASSWORD_REQUIRED,
                UserRegistrationFaultKeys.MSG_PASSWORD_REQUIRED,
                UserRegistrationFaultKeys.FALLBACK_PASSWORD_REQUIRED);
        }

        private static string EnsureDisplayNameIsProvided(string displayName)
        {
            string normalizedDisplayName = (displayName ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(normalizedDisplayName))
            {
                return normalizedDisplayName;
            }

            throw FaultsFactory.Create(
                UserRegistrationFaultKeys.CODE_DISPLAYNAME_REQUIRED,
                UserRegistrationFaultKeys.MSG_DISPLAYNAME_REQUIRED,
                UserRegistrationFaultKeys.FALLBACK_DISPLAYNAME_REQUIRED);
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

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        protected override FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }

        private NormalizedRegistration ValidateAndNormalize(RegisterUserArgs userArgs)
        {
            EnsureArgsNotNull(userArgs);

            string email = EnsureEmailIsProvided(userArgs.Email);
            string displayName = EnsureDisplayNameIsProvided(userArgs.DisplayName);
            string password = EnsurePasswordIsProvided(userArgs.Password);
            DateTime nowUtc = EnsureNowUtcIsValid(userArgs.NowUtc);

            return new NormalizedRegistration(email, displayName, password, nowUtc);
        }

        private RegistrationDbResult CreateAccountAndToken(NormalizedRegistration registration)
        {
            byte[] passwordHash = passwordHasher.HashPassword(registration.Password);

            string defaultAvatarId = avatarRepository.GetDefaultAvatarId();

            if (string.IsNullOrWhiteSpace(defaultAvatarId))
            {
                Logger.Error(LOG_CTX_REGISTER + ": default avatar id not configured.");

                throw FaultsFactory.Create(
                    InfrastructureFaultKeys.CODE_DEFAULT_AVATAR_NOT_CONFIGURED,
                    InfrastructureFaultKeys.MSG_DEFAULT_AVATAR_NOT_CONFIGURED,
                    InfrastructureFaultKeys.FALLBACK_DEFAULT_AVATAR_NOT_CONFIGURED);
            }

            var createAccountArgs = new CreateAccountArgs
            {
                Email = registration.Email,
                PasswordHash = passwordHash,
                DisplayName = registration.DisplayName,
                CreationDate = registration.NowUtc,
                AvatarId = defaultAvatarId
            };

            VerificationCodeResult codeResult = verificationCodeService.CreateVerificationCodeOrFault();

            using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
            using (IGuessWhoDbTransaction tx = unitOfWork.BeginTransaction())
            {
                if (unitOfWork.UserAccounts.EmailExists(registration.Email))
                {
                    Logger.WarnFormat("{0}: email already exists '{1}'.", LOG_CTX_REGISTER, registration.Email);

                    throw FaultsFactory.Create(
                        UserRegistrationFaultKeys.CODE_EMAIL_ALREADY_EXISTS,
                        UserRegistrationFaultKeys.MSG_EMAIL_ALREADY_EXISTS,
                        UserRegistrationFaultKeys.FALLBACK_EMAIL_ALREADY_EXISTS);
                }

                var created = unitOfWork.UserAccounts.CreateAccount(createAccountArgs);

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

                tx.Commit();

                return new RegistrationDbResult(created.Account, created.Profile, codeResult);
            }
        }

        private void TrySendVerificationEmail(string email, string code, long accountId, int expirationMinutes)
        {
            var message = verificationCodeEmailBuilder.Build(new VerificationCodeEmailContext(email, code, expirationMinutes));

            EmailSendResult sendResult = emailSender.Send(message);

            if (!sendResult.IsSuccess)
            {
                Logger.WarnFormat(
                    "{0}: verification email send failed for accountId '{1}'. Status='{2}', ErrorCode='{3}'.",
                    LOG_CTX_REGISTER,
                    accountId,
                    sendResult.Status,
                    sendResult.ErrorCode);

                if (sendResult.TechnicalException != null)
                {
                    Logger.Error(LOG_CTX_REGISTER + ": email technical exception.", sendResult.TechnicalException);
                }
            }
        }

        private sealed class NormalizedRegistration
        {
            public NormalizedRegistration(string email, string displayName, string password, DateTime nowUtc)
            {
                Email = email;
                DisplayName = displayName;
                Password = password;
                NowUtc = nowUtc;
            }

            public string Email { get; }
            public string DisplayName { get; }
            public string Password { get; }
            public DateTime NowUtc { get; }
        }

        private sealed class RegistrationDbResult
        {
            public RegistrationDbResult(
                AccountRecord account,
                UserProfileRecord profile,
                VerificationCodeResult verificationCode)
            {
                Account = account;
                Profile = profile;
                VerificationCode = verificationCode;
            }

            public AccountRecord Account { get; }
            public UserProfileRecord Profile { get; }
            public VerificationCodeResult VerificationCode { get; }
        }
    }
}
