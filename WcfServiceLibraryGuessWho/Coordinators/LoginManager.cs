using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Enums;
using GuessWhoServices.Repositories.Interfaces;
using System;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class LoginResult
    {
        public LoginResult(long userId, string displayName, string email)
        {
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            Email = email ?? string.Empty;
        }

        public long UserId { get; }
        public string DisplayName { get; }
        public string Email { get; }
    }

    public sealed class LoginManager : ILoginManager
    {
        private readonly IUserAccountRepository accountRepository;
        private readonly IGameSessionRepository gameSessionRepository;

        public LoginManager(
            IUserAccountRepository accountRepository,
            IGameSessionRepository gameSessionRepository)
        {
            this.accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
            this.gameSessionRepository = gameSessionRepository ??
                throw new ArgumentNullException(nameof(gameSessionRepository));
        }

        public LoginResult Login(LoginArgs loginArgs)
        {
            if (loginArgs == null)
            {
                throw new ArgumentNullException(nameof(loginArgs),
                    LoginServiceFaults.ERROR_MESSAGE_ARGS_REQUIRED);
            }

            string email = EnsureEmailIsProvided(loginArgs.Email);
            string password = EnsurePasswordIsProvided(loginArgs.Password);

            var searchParams = new AccountSearchParameters { Email = email };
            var result = accountRepository.GetAccountWithProfileForLogin(searchParams);

            if (result == null || result.Status == AccountProfileStatus.NotFoundOrDeleted)
            {
                throw new InvalidOperationException(LoginServiceFaults.ERROR_MESSAGE_ACCOUNT_NOT_FOUND);
            }

            if (result.Status == AccountProfileStatus.Locked)
            {
                throw new InvalidOperationException(LoginServiceFaults.ERROR_MESSAGE_ACCOUNT_LOCKED);
            }

            if (result.Status == AccountProfileStatus.ProfileAlreadyActive)
            {
                throw new InvalidOperationException(LoginServiceFaults.ERROR_MESSAGE_PROFILE_ALREADY_ACTIVE);
            }

            bool isPasswordValid = PasswordHasher.Verify(password, result.Account.PasswordHash);
            if (!isPasswordValid)
            {
                throw new InvalidOperationException(LoginServiceFaults.ERROR_MESSAGE_INVALID_PASSWORD);
            }

            bool updated = accountRepository.UpdateLastLoginUtc(searchParams);
            if (!updated)
            {
                throw new InvalidOperationException(LoginServiceFaults.ERROR_MESSAGE_UPDATE_LOGIN_FAILED);
            }

            gameSessionRepository.ForceLeaveActiveSessionsForUser(result.Profile.UserId);

            return new LoginResult(
                result.Profile.UserId,
                result.Profile.DisplayName,
                result.Account.Email);
        }

        public bool Logout(long userProfileId)
        {
            if (userProfileId <= 0)
            {
                return false;
            }

            return accountRepository.MarkUserProfileInactive(userProfileId);
        }

        private static string EnsureEmailIsProvided(string email)
        {
            string normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new ArgumentException(
                    LoginServiceFaults.ERROR_MESSAGE_EMAIL_REQUIRED,
                    nameof(LoginArgs.Email));
            }

            return normalizedEmail;
        }

        private static string EnsurePasswordIsProvided(string password)
        {
            string safePassword = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(safePassword))
            {
                throw new ArgumentException(
                    LoginServiceFaults.ERROR_MESSAGE_PASSWORD_REQUIRED,
                    nameof(LoginArgs.Password));
            }

            return safePassword;
        }
    }
}