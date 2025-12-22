using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWho.Services.WCF.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using GuessWhoServices.Repositories.Interfaces;
using System;
using WcfServiceLibraryGuessWho.Coordinators.FaultsCatalogs;
using WcfServiceLibraryGuessWho.Coordinators.Interfaces;
using WcfServiceLibraryGuessWho.Coordinators.Parameters;

namespace WcfServiceLibraryGuessWho.Coordinators
{
    public sealed class LoginManager : ILoginManager
    {
        private readonly IUserAccountRepository _accountRepository;

        public LoginManager(IUserAccountRepository accountRepository)
        {
            _accountRepository = accountRepository ??
                throw new ArgumentNullException(nameof(accountRepository));
        }

        public UserSessionLoginResult Login(LoginArgs loginArgs)
        {
            ValidateLoginArgs(loginArgs);

            var searchParams = new AccountSearchParameters
            {
                Email = loginArgs.Email.Trim().ToLowerInvariant()
            };

            var result = _accountRepository.GetAccountWithProfileForLogin(searchParams);

            if (result == null || result.Status == AccountProfileStatus.NotFoundOrDeleted)
            {
                return UserSessionLoginResult.CreateFailed(UserSessionLoginStatus.InvalidCredentials);
            }

            if (result.Status == AccountProfileStatus.Locked)
            {
                return UserSessionLoginResult.CreateFailed(UserSessionLoginStatus.AccountLocked);
            }

            bool isPasswordValid = PasswordHasher.Verify(loginArgs.Password, result.Account.PasswordHash);
            if (!isPasswordValid)
            {
                return UserSessionLoginResult.CreateFailed(UserSessionLoginStatus.InvalidCredentials);
            }

            _accountRepository.UpdateLastLoginUtc(searchParams);

            return UserSessionLoginResult.CreateSuccessful(result.Account, result.Profile);
        }

        private void ValidateLoginArgs(LoginArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args), LoginServiceFaults.ERROR_MESSAGE_ARGS_REQUIRED);

            if (string.IsNullOrWhiteSpace(args.Email))
                throw new ArgumentException(LoginServiceFaults.ERROR_MESSAGE_EMAIL_REQUIRED);

            if (string.IsNullOrWhiteSpace(args.Password))
                throw new ArgumentException(LoginServiceFaults.ERROR_MESSAGE_PASSWORD_REQUIRED);
        }
    }
}