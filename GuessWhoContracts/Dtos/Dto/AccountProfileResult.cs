using GuessWhoContracts.Enums;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class AccountProfileResult
    {
        public AccountDto Account { get; private set; }
        public UserProfileDto Profile { get; private set; }
        public AccountProfileStatus Status { get; private set; }

        private AccountProfileResult()
        {
        }

        public static AccountProfileResult Ok(AccountDto account, UserProfileDto profile)
        {
            return new AccountProfileResult
            {
                Account = account,
                Profile = profile,
                Status = AccountProfileStatus.Success
            };
        }

        public static AccountProfileResult Fail(AccountProfileStatus status)
        {
            return new AccountProfileResult
            {
                Account = AccountDto.CreateInvalid(),
                Profile = UserProfileDto.CreateInvalid(),
                Status = status
            };
        }
    }

}
