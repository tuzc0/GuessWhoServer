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
        private const string LOG_CTX_REGISTER_GUEST = "UserRegistrationManager.RegisterGuest";
        private const string GUEST_TAG_FORMAT = "D6";

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

            throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_ARGS_REQUIRED);
        }

        private static DateTime EnsureNowUtcIsValid(DateTime nowUtc)
        {
            if (nowUtc != default)
            {
                return nowUtc;
            }

            throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_NOWUTC_REQUIRED);
        }

        private void ThrowValidationFault(ValidationError error)
        {
            string key = error != null ? error.Key ?? EMPTY : EMPTY;

            switch (key)
            {
                case UserValidationCodes.INVALID_REQUEST:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_ARGS_REQUIRED);

                case UserValidationCodes.EMAIL_REQUIRED:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_EMAIL_REQUIRED);

                case UserValidationCodes.EMAIL_TOO_LONG:
                case UserValidationCodes.EMAIL_INVALID_FORMAT:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_EMAIL_INVALID);

                case UserValidationCodes.DISPLAY_NAME_REQUIRED:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_DISPLAYNAME_REQUIRED);

                case UserValidationCodes.DISPLAY_NAME_TOO_SHORT:
                case UserValidationCodes.DISPLAY_NAME_TOO_LONG:
                case UserValidationCodes.DISPLAY_NAME_INVALID_FORMAT:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_DISPLAYNAME_INVALID);

                case UserValidationCodes.PASSWORD_REQUIRED:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_PASSWORD_REQUIRED);

                case UserValidationCodes.PASSWORD_TOO_SHORT:
                case UserValidationCodes.PASSWORD_TOO_LONG:
                case UserValidationCodes.CONFIRM_PASSWORD_REQUIRED:
                case UserValidationCodes.CONFIRM_PASSWORD_MISMATCH:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_PASSWORD_INVALID);

                default:
                    throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_VALIDATION_FAILED);
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
            EnsureCreateAccountInputsOrThrow(unitOfWork, registration);

            byte[] passwordHash = HashPassword(registration.Password);
            VerificationCodeResult codeResult = CreateVerificationCodeOrThrow();

            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            string defaultAvatarId = GetDefaultAvatarIdOrThrow(unitOfWork);

            CreateAccountArgs createAccountArgs = BuildCreateAccountArgs(
                registration,
                passwordHash,
                defaultAvatarId);

            CreatedAccountResult created = CreateAccountOrThrowEmailExists(
                unitOfWork,
                createAccountArgs,
                registration.Email);

            ResetEmailVerificationState(unitOfWork, created.Account.AccountId, registration.NowUtc);

            CreateEmailTokenArgs tokenArgs = BuildCreateEmailTokenArgs(
                created.Account.AccountId,
                codeResult.HashCode,
                registration.NowUtc);

            AddVerificationTokenOrThrow(unitOfWork, tokenArgs, created.Account.AccountId);

            Commit(unitOfWork, transaction);

            return BuildRegistrationDbResult(created, codeResult);
        }

        private static void EnsureCreateAccountInputsOrThrow(IGuessWhoUnitOfWork unitOfWork, NormalizedRegistration registration)
        {
            if (unitOfWork == null)
            {
                throw new ArgumentNullException(nameof(unitOfWork));
            }

            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }
        }

        private byte[] HashPassword(string password)
        {
            return passwordHasher.HashPassword(password ?? EMPTY);
        }

        private VerificationCodeResult CreateVerificationCodeOrThrow()
        {
            return verificationCodeService.CreateVerificationCodeOrFault();
        }

        private string GetDefaultAvatarIdOrThrow(IGuessWhoUnitOfWork unitOfWork)
        {
            string defaultAvatarId = unitOfWork.Avatars.GetDefaultAvatarId();

            if (!string.IsNullOrWhiteSpace(defaultAvatarId))
            {
                return defaultAvatarId;
            }

            Logger.ErrorFormat("{0}: default avatar id not configured.", LOG_CTX_REGISTER);
            throw FaultsFactory.Create(InfrastructureFaultKeys.CODE_DEFAULT_AVATAR_NOT_CONFIGURED);
        }

        private static CreateAccountArgs BuildCreateAccountArgs(
            NormalizedRegistration registration,
            byte[] passwordHash,
            string defaultAvatarId)
        {
            return new CreateAccountArgs
            {
                Email = registration.Email,
                PasswordHash = passwordHash ?? Array.Empty<byte>(),
                DisplayName = registration.DisplayName,
                CreationDate = registration.NowUtc,
                AvatarId = defaultAvatarId
            };
        }

        private CreatedAccountResult CreateAccountOrThrowEmailExists(
            IGuessWhoUnitOfWork unitOfWork,
            CreateAccountArgs createAccountArgs,
            string emailForLog)
        {
            CreatedAccountResult created = unitOfWork.UserAccounts.CreateAccount(createAccountArgs);

            if (created != null && created.Account != null && created.Account.IsValid)
            {
                return created;
            }

            Logger.WarnFormat("{0}: email already exists '{1}'.", LOG_CTX_REGISTER, emailForLog ?? EMPTY);
            throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_EMAIL_ALREADY_EXISTS);
        }

        private static void ResetEmailVerificationState(IGuessWhoUnitOfWork unitOfWork, long accountId, DateTime nowUtc)
        {
            unitOfWork.EmailVerification.ConsumeActiveTokens(
                new GuessWhoServerDomain.Domain.Parameters.EmailVerification.ConsumeActiveTokensArgs
                {
                    AccountId = accountId,
                    ConsumedUtc = nowUtc
                });
        }

        private CreateEmailTokenArgs BuildCreateEmailTokenArgs(long accountId, byte[] codeHash, DateTime nowUtc)
        {
            return new CreateEmailTokenArgs
            {
                AccountId = accountId,
                CodeHash = codeHash ?? Array.Empty<byte>(),
                NowUtc = nowUtc,
                LifeSpan = verificationCodeLifeTime
            };
        }

        private void AddVerificationTokenOrThrow(IGuessWhoUnitOfWork unitOfWork, CreateEmailTokenArgs tokenArgs, long accountId)
        {
            bool tokenCreated = unitOfWork.EmailVerification.AddVerificationToken(tokenArgs);

            if (tokenCreated)
            {
                return;
            }

            Logger.WarnFormat("{0}: token creation failed for accountId '{1}'.", LOG_CTX_REGISTER, accountId);
            throw FaultsFactory.Create(UserRegistrationFaultKeys.CODE_TOKEN_CREATION_FAILED);
        }

        private static void Commit(IGuessWhoUnitOfWork unitOfWork, IGuessWhoDbTransaction transaction)
        {
            unitOfWork.Flush();
            transaction.Commit();
        }

        private static RegistrationDbResult BuildRegistrationDbResult(CreatedAccountResult created, VerificationCodeResult codeResult)
        {
            return new RegistrationDbResult(created.Account, created.Profile, codeResult);
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

        public RegisterResult RegisterGuest(string requestedName)
        {
            return ExecuteService(
                LOG_CTX_REGISTER_GUEST,
                () =>
                {
                    string uniqueTag = GenerateGuestTag();
                    string finalName = BuildGuestDisplayName(requestedName, uniqueTag);

                    using (IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create())
                    {
                        string defaultAvatarId = unitOfWork.Avatars.GetDefaultAvatarId();

                        if (string.IsNullOrWhiteSpace(defaultAvatarId))
                        {
                            throw FaultsFactory.Create(InfrastructureFaultKeys.CODE_DEFAULT_AVATAR_NOT_CONFIGURED);
                        }

                        var guestProfile = new UserProfileRecord
                        {
                            DisplayName = finalName,
                            AvatarId = defaultAvatarId,
                            IsGuest = true,
                            IsActive = true,
                            CreatedAtUtc = DateTime.UtcNow
                        };

                        long userId = unitOfWork.UserProfiles.AddUserProfile(guestProfile);
                        unitOfWork.Flush();

                        var resultArgs = new RegisterResultArgs(0, userId, EMPTY)
                        {
                            DisplayName = finalName,
                            EmailVerificationRequired = false
                        };

                        return new RegisterResult(resultArgs);
                    }
                });
        }

        private string GenerateGuestTag()
        {
            byte[] buffer = new byte[4];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            int randomInt = BitConverter.ToInt32(buffer, 0) & int.MaxValue;
            return (randomInt % 1000000).ToString(GUEST_TAG_FORMAT);
        }

        private static string BuildGuestDisplayName(string requestedName, string tag)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                return $"Guest#{tag}";
            }

            string baseName = requestedName.Trim();
            if (baseName.Length > 15)
            {
                baseName = baseName.Substring(0, 15);
            }

            return $"{baseName}#{tag}";
        }
    }
}
