using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using ClassLibraryGuessWho.Data.Factories;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;

namespace GuessWhoServices.Repositories.Implementation
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly IGuessWhoDbContextFactory contextFactory;

        public UserAccountRepository(IGuessWhoDbContextFactory contextFactory)
        {
            this.contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        private T Execute<T>(Func<IUserAccountData, T> action)
        {
            using (var context = contextFactory.Create())
            {
                var userAccountData = new UserAccountData(context);
                return action(userAccountData);
            }
        }

        public bool EmailExist(string email) => Execute(
            userAccountData => userAccountData.EmailExists(email));

        public (AccountDto account, UserProfileDto userProfile) CreateAccount(
            CreateAccountArgs createAccountArgs) => Execute(UserAccountData =>
                UserAccountData.CreateAccount(createAccountArgs));

        public AccountDto GetAccountByIdAccount(long accountId) => Execute(
            userAccountData => userAccountData.GetAccountByIdAccount(accountId));

        public (AccountDto account, UserProfileDto userProfile)
            GetAccountWithProfileByIdAccount(long accountId) => Execute(
                userAccountData => userAccountData.GetAccountWithProfileByIdAccount(accountId));

        public bool MarkEmailVerified(long accountId, DateTime nowUtc) => Execute(
            userAccountData => userAccountData.MarkEmailVerified(accountId, nowUtc));

        public long GetAccountIdByEmail(string email) => Execute(
            userAccountData => userAccountData.GetAccountIdByEmail(email));

        public bool UpdatePasswordOnly(long accountId, byte[] newPassword) => Execute(
            userAccountData => userAccountData.UpdatePasswordOnly(accountId, newPassword));

        public (AccountDto account, UserProfileDto userProfile)
            UpdateDisplayNameAndPassword(UpdateAccountArgs updateAccountArgs) =>
            Execute(userAccountData => userAccountData.UpdateDisplayNameAndPassword(updateAccountArgs));

        public AccountProfileResult GetAccountWithProfileForLogin(AccountSearchParameters accountSearchParameters)
            => Execute(userAccountData => userAccountData.GetAccountWithProfileForLogin(accountSearchParameters));

        public (AccountDto account, UserProfileDto userProfile) TryGetAccountWithProfileForUpdate(
            AccountSearchParameters accountSearchParameters) => Execute(
                userAccountData => userAccountData.TryGetAccountWithProfileForUpdate(accountSearchParameters));

        public bool UpdateLastLoginUtc(AccountSearchParameters accountSearchParameters) => Execute(
            userAccountData => userAccountData.UpdateLastLoginUtc(accountSearchParameters));

        public bool DeleteAccount(long accountId) => Execute(
            userAccountData => userAccountData.DeleteAccount(accountId));

        public bool MarkUserProfileActive(long userProfileId) => Execute(
            userAccountData => userAccountData.MarkUserProfileActive(userProfileId));

        public bool MarkUserProfileInactive(long userProfileId) => Execute(
            userAccountData => userAccountData.MarkUserProfileInactive(userProfileId));
    }
}