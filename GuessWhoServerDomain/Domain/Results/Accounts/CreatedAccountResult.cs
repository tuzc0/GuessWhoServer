using GuessWhoServerDomain.Domain.Models.Accounts;

namespace GuessWhoServerDomain.Domain.Results.Accounts
{
    public sealed class CreatedAccountResult
    {
        public AccountRecord Account { get; private set; }
        public UserProfileRecord Profile { get; private set; }

        private CreatedAccountResult() { }

        public static CreatedAccountResult Ok(AccountRecord account, UserProfileRecord profile)
        {
            return new CreatedAccountResult
            {
                Account = account ?? AccountRecord.CreateInvalid(),
                Profile = profile ?? UserProfileRecord.CreateInvalid()
            };
        }
    }
}
