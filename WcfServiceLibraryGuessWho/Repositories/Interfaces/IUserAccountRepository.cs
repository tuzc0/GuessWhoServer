using ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters;
using GuessWhoContracts.Dtos.Dto;
using System; 

namespace GuessWhoServices.Repositories.Interfaces
{
    public interface IUserAccountRepository
    {
        bool EmailExist(string email);

        (AccountDto account, UserProfileDto userProfile) CreateAccount(CreateAccountArgs createAccountArgs);

        AccountDto GetAccountByIdAccount(long accountId);

        (AccountDto account, UserProfileDto userProfile) GetAccountWithProfileByIdAccount(long accountId);

        bool MarkEmailVerified(long accountId, DateTime nowUtc);

        long GetAccountIdByEmail(string email);

        bool UpdatePasswordOnly(long accountId, byte[] newPassword);

        (AccountDto account, UserProfileDto userProfile) UpdateDisplayNameAndPassword(UpdateAccountArgs updateAccountArgs);

        AccountProfileResult GetAccountWithProfileForLogin(AccountSearchParameters accountSearchParameters);

        (AccountDto account, UserProfileDto userProfile) TryGetAccountWithProfileForUpdate(AccountSearchParameters accountSearchParameters);

        bool UpdateLastLoginUtc(AccountSearchParameters accountSearchParameters);

        bool DeleteAccount(long accountId);

        bool MarkUserProfileInactive(long userProfileId);
    }
}
