using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWhoContracts.Dtos.Dto;
using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public interface IUserAccountData
    {
        bool EmailExists(string email);
        (AccountDto account, UserProfileDto profile) CreateAccount(CreateAccountArgs args);
        AccountDto GetAccountByIdAccount(long accountId);
        bool MarkEmailVerified(long accountId, DateTime nowUtc);
        AccountProfileResult GetAccountWithProfileForLogin(AccountSearchParameters args);
        (AccountDto account, UserProfileDto profile) TryGetAccountWithProfileForUpdate(AccountSearchParameters args);
        bool UpdateLastLoginUtc(AccountSearchParameters args);
        bool DeleteAccount(long userId);
        (AccountDto account, UserProfileDto profile) UpdateDisplayNameAndPassword(UpdateAccountArgs args);
        (AccountDto account, UserProfileDto profile) GetAccountWithProfileByIdAccount(long userId);
        long GetAccountIdByEmail(string email);
        bool UpdatePasswordOnly(long accountId, byte[] newPasswordHash);
    }
}
