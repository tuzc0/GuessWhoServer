using GuessWhoServerDomain.Domain.Models.Accounts;

namespace GuessWhoServerDomain.Domain.Results.Accounts
{
    public sealed class UpdatedAccountResult
    {
        public bool IsSuccess { get; private set; }
        public AccountRecord Account { get; private set; }
        public UserProfileRecord Profile { get; private set; }

        private UpdatedAccountResult()
        {
            Account = AccountRecord.CreateInvalid();
            Profile = UserProfileRecord.CreateInvalid();
        }

        public static UpdatedAccountResult Ok(AccountRecord account, UserProfileRecord profile)
        {
            return new UpdatedAccountResult
            {
                IsSuccess = true,
                Account = account ?? AccountRecord.CreateInvalid(),
                Profile = profile ?? UserProfileRecord.CreateInvalid()
            };
        }

        public static UpdatedAccountResult Fail()
        {
            return new UpdatedAccountResult
            {
                IsSuccess = false,
                Account = AccountRecord.CreateInvalid(),
                Profile = UserProfileRecord.CreateInvalid()
            };
        }
    }
}
