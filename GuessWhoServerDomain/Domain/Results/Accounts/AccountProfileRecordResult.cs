using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Models.Accounts;

namespace GuessWhoServerDomain.Domain.Results.Accounts
{
    public sealed class AccountProfileRecordResult
    {
        public AccountRecord Account { get; private set; }
        public UserProfileRecord Profile { get; private set; }
        public AccountProfileStatus Status { get; private set; }

        private AccountProfileRecordResult()
        {
            Account = AccountRecord.CreateInvalid();
            Profile = UserProfileRecord.CreateInvalid();
            Status = AccountProfileStatus.NotFoundOrDeleted;
        }

        public static AccountProfileRecordResult Ok(AccountRecord account, UserProfileRecord profile)
        {
            return new AccountProfileRecordResult
            {
                Account = account ?? AccountRecord.CreateInvalid(),
                Profile = profile ?? UserProfileRecord.CreateInvalid(),
                Status = AccountProfileStatus.Success
            };
        }

        public static AccountProfileRecordResult Fail(AccountProfileStatus status)
        {
            return new AccountProfileRecordResult
            {
                Account = AccountRecord.CreateInvalid(),
                Profile = UserProfileRecord.CreateInvalid(),
                Status = status
            };
        }
    }
}
