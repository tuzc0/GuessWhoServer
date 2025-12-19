using ClassLibraryGuessWho.Data;
using ClassLibraryGuessWho.Data.DataAccess.Accounts;
using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoServices.Repositories.Interfaces;
using System;

namespace GuessWhoServices.Repositories.Implementation
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly UserAccountData userAccountData; 

        public UserAccountRepository(UserAccountData userAccountData)
        {
            this.userAccountData = userAccountData ?? 
                throw new ArgumentNullException(nameof(userAccountData));
        }

        public UserAccountRepository(GuessWhoDBEntities dataContext): 
            this(new UserAccountData(dataContext)) 
        {
        }

        public bool EmailExist(string email)
        {
            return userAccountData.EmailExists(email);
        }

        public (AccountDto account, UserProfileDto userProfile) CreateAccount(
            CreateAccountArgs createAccountArgs)
        {
            return userAccountData.CreateAccount(createAccountArgs);
        }

        public AccountDto GetAccountByIdAccount(long accountId)
        {
            return userAccountData.GetAccountByIdAccount(accountId);
        }

        public (AccountDto account, UserProfileDto userProfile) 
            GetAccountWithProfileByIdAccount(long accountId)
        {
            return userAccountData.GetAccountWithProfileByIdAccount(accountId);
        }

        public bool MarkEmailVerified(long accountId, DateTime nowUtc)
        {
            return userAccountData.MarkEmailVerified(accountId, nowUtc);
        }

        public long GetAccountIdByEmail(string email)
        {
            return userAccountData.GetAccountIdByEmail(email);
        }
        
        public bool UpdatePasswordOnly(long accountId, byte[] newPassword)
        {
            return userAccountData.UpdatePasswordOnly(accountId, newPassword);
        }

        public (AccountDto account, UserProfileDto userProfile) 
            UpdateDisplayNameAndPassword(UpdateAccountArgs updateAccountArgs)
        {
            return userAccountData.UpdateDisplayNameAndPassword(updateAccountArgs);
        }

        public AccountProfileResult GetAccountWithProfileForLogin(AccountSearchParameters accountSearchParameters)
        {
            return userAccountData.GetAccountWithProfileForLogin(accountSearchParameters);
        }

        public (AccountDto account, UserProfileDto userProfile) TryGetAccountWithProfileForUpdate(
            AccountSearchParameters accountSearchParameters)
        {
            return userAccountData.TryGetAccountWithProfileForUpdate(accountSearchParameters);
        }

        public bool UpdateLastLoginUtc(AccountSearchParameters accountSearchParameters)
        {
            return userAccountData.UpdateLastLoginUtc(accountSearchParameters);
        }

        public bool DeleteAccount(long accountId)
        {
            return userAccountData.DeleteAccount(accountId);
        }
    }
}
