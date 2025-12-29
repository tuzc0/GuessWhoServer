using GuessWhoServerDomain.Domain.Models.Accounts;

namespace GuessWhoServerDomain.Domain.Results.Accounts
{
    public sealed class AccountWithProfileResult
    {
        public AccountRecord Account { get; private set; }
        public UserProfileRecord Profile { get; private set; }
        public bool IsFound { get; private set; }

        private AccountWithProfileResult() { }

        public static AccountWithProfileResult Found(AccountRecord account, UserProfileRecord profile)
        {
            return new AccountWithProfileResult
            {
                Account = account ?? AccountRecord.CreateInvalid(),
                Profile = profile ?? UserProfileRecord.CreateInvalid(),
                IsFound = true
            };
        }

        public static AccountWithProfileResult NotFound()
        {
            return new AccountWithProfileResult
            {
                Account = AccountRecord.CreateInvalid(),
                Profile = UserProfileRecord.CreateInvalid(),
                IsFound = false
            };
        }
    }
}
