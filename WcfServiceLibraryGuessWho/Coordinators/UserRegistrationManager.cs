using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters;
using GuessWho.Services.Security;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;
using WcfServiceLibraryGuessWho.Communication.Email;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters;

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

    public sealed class UserRegistrationManager : IUserRegistrationManager
    {
        private static readonly TimeSpan VerificationCodeLifeTime =
            TimeSpan.FromMinutes(UserRegistrationFaults.VERIFICATION_CODE_EXPIRY_MINUTES);

        private readonly IUserAccountRepository accountRepository;
        private readonly IEmailVerificationRepository emailRepository;
        private readonly IAvatarRepository avatarRepository;
        private readonly IVerificationEmailSender emailSender;

        public UserRegistrationManager(
            IUserAccountRepository accountRepository,
            IEmailVerificationRepository emailRepository,
            IAvatarRepository avatarRepository,
            IVerificationEmailSender emailSender)
        {
            this.accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
            this.emailRepository = emailRepository ??
                throw new ArgumentNullException(nameof(emailRepository));
            this.avatarRepository = avatarRepository ??
                throw new ArgumentNullException(nameof(avatarRepository));
            this.emailSender = emailSender ??
                throw new ArgumentNullException(nameof(emailSender));
        }

        public RegisterResult RegisterUser(RegisterUserArgs registerUserArgs)
        {
            if (registerUserArgs == null)
            {
                throw new ArgumentNullException(nameof(registerUserArgs),
                    UserRegistrationFaults.ERROR_MESSAGE_ARGS_REQUIRED);
            }

            string email = EnsureEmailIsProvidedAndUnique(registerUserArgs.Email);
            string displayName = EnsureDisplayNameIsProvided(registerUserArgs.DisplayName);
            string password = EnsurePasswordIsProvided(registerUserArgs.Password);
            DateTime nowUtc = EnsureNowUtcIsValid(registerUserArgs.NowUtc);

            byte[] passwordHash = PasswordHasher.HashPassword(password);
            string defaultAvatarId = avatarRepository.GetDefaultAvatarId();

            var createAccountArgs = new CreateAccountArgs
            {
                Email = email,
                Password = passwordHash,
                DisplayName = displayName,
                CreationDate = nowUtc,
                AvatarId = defaultAvatarId
            };

            var created = accountRepository.CreateAccount(createAccountArgs);

            AccountDto account = created.account;
            UserProfileDto profile = created.userProfile;

            VerificationCodeResult codeResult = CreateVerificationCode();

            var tokenArgs = new CreateEmailTokenArgs
            {
                AccountId = account.AccountId,
                CodeHash = codeResult.HashCode,
                NowUtc = nowUtc,
                LifeSpan = VerificationCodeLifeTime
            };

            bool tokenCreated = emailRepository.AddVerificationToken(tokenArgs);

            if (!tokenCreated)
            {
                throw new InvalidOperationException(
                    UserRegistrationFaults.ERROR_MESSAGE_TOKEN_CREATION_FAILED);
            }

            emailSender.SendVerificationCode(email, codeResult.PlainCode);

            return new RegisterResult(
                account.AccountId,
                profile.UserId,
                email,
                profile.DisplayName,
                true);
        }

        private string EnsureEmailIsProvidedAndUnique(string email)
        {
            string normalizedEmail = NormalizeEmail(email);

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new ArgumentException(
                    UserRegistrationFaults.ERROR_MESSAGE_EMAIL_REQUIRED, 
                    nameof(RegisterUserArgs.Email));
            }

            if (accountRepository.EmailExist(normalizedEmail))
            {
                throw new InvalidOperationException(UserRegistrationFaults.ERROR_MESSAGE_EMAIL_ALREADY_EXISTS);
            }

            return normalizedEmail;
        }

        private static string EnsurePasswordIsProvided(string password)
        {
            string safePassword = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(safePassword))
            {
                throw new ArgumentException(
                    UserRegistrationFaults.ERROR_MESSAGE_PASSWORD_REQUIRED, 
                    nameof(RegisterUserArgs.Password));
            }

            return safePassword;
        }

        private static string EnsureDisplayNameIsProvided(string displayName)
        {
            string normalizedDisplayName = (displayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedDisplayName))
            {
                throw new ArgumentException(
                    UserRegistrationFaults.ERROR_MESSAGE_DISPLAYNAME_REQUIRED, 
                    nameof(RegisterUserArgs.DisplayName));
            }

            return normalizedDisplayName;
        }

        private static DateTime EnsureNowUtcIsValid(DateTime nowUtc)
        {
            if (nowUtc == default)
            {
                throw new ArgumentException(
                    UserRegistrationFaults.ERROR_MESSAGE_NOWUTC_REQUIRED, 
                    nameof(RegisterUserArgs.NowUtc));
            }

            return nowUtc;
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static VerificationCodeResult CreateVerificationCode()
        {
            string code = CodeGenerator.GenerateNumericCode();
            byte[] hashCode = CodeGenerator.ComputeSha256Hash(code);

            return new VerificationCodeResult(code, hashCode);
        }
    }
}
