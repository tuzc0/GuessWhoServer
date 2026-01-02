using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Models.Accounts;
using System;

namespace GuessWhoServerDomain.Domain.Results.Sessions
{
    public sealed class AccountWithProfileForLoginResult
    {
        public AccountProfileStatus Status { get; }
        public AccountRecord Account { get; }
        public UserProfileRecord Profile { get; }

        public bool IsSuccess => Status == AccountProfileStatus.Success;

        private AccountWithProfileForLoginResult(
            AccountProfileStatus status,
            AccountRecord account,
            UserProfileRecord profile)
        {
            Status = status;
            Account = account ?? AccountRecord.CreateInvalid();
            Profile = profile ?? UserProfileRecord.CreateInvalid();
        }

        public static AccountWithProfileForLoginResult CreateNotFoundOrDeleted()
        {
            return new AccountWithProfileForLoginResult(
                AccountProfileStatus.NotFoundOrDeleted,
                AccountRecord.CreateInvalid(),
                UserProfileRecord.CreateInvalid());
        }

        public static AccountWithProfileForLoginResult CreateLocked(AccountRecord account, UserProfileRecord profile)
        {
            return new AccountWithProfileForLoginResult(
                AccountProfileStatus.Locked,
                account,
                profile);
        }

        public static AccountWithProfileForLoginResult CreateSuccessful(AccountRecord account, UserProfileRecord profile)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return new AccountWithProfileForLoginResult(
                AccountProfileStatus.Success,
                account,
                profile);
        }
    }
}
