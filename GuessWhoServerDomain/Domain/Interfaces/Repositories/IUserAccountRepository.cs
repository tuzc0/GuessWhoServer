using GuessWhoServerDomain.Domain.Models.Accounts;
using GuessWhoServerDomain.Domain.Parameters.Accounts;
using GuessWhoServerDomain.Domain.Results.Accounts;
using System;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IUserAccountRepository
    {
        bool EmailExists(string email);

        CreatedAccountResult CreateAccount(CreateAccountArgs createAccountArgs);

        AccountRecord GetAccountByIdAccount(long accountId);

        AccountWithProfileResult GetAccountWithProfileByUserId(long userId);

        bool MarkEmailVerified(long accountId, DateTime nowUtc);

        long GetAccountIdByEmail(string email);

        bool UpdatePasswordOnly(UpdatePasswordArgs passwordUpdateArgs);

        UpdatedAccountResult UpdateDisplayNameAndPassword(UpdateAccountArgs updateAccountArgs);

        AccountProfileRecordResult GetAccountWithProfileForLogin(
            AccountSearchParameters accountSearchParameters,
            DateTime nowUtc);

        AccountWithProfileResult TryGetAccountWithProfileForUpdate(
            AccountSearchParameters accountSearchParameters);

        bool UpdateLastLoginUtc(
            AccountSearchParameters accountSearchParameters,
            DateTime nowUtc);

        bool DeleteAccount(long userId, DateTime nowUtc);

        bool MarkUserProfileActive(long userId);

        bool MarkUserProfileInactive(long userId);
    }
}
