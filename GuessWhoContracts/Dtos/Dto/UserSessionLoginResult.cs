using GuessWhoContracts.Enums;

namespace GuessWhoContracts.Dtos.Dto
{
    public class UserSessionLoginResult
    {
        public UserSessionLoginStatus Status { get; private set; }

        public AccountDto Account { get; private set; }

        public UserProfileDto Profile { get; private set; }

        private UserSessionLoginResult()
        {
        }

        public static UserSessionLoginResult Ok(AccountDto account, UserProfileDto profile)
        {
            return new UserSessionLoginResult
            {
                Status = UserSessionLoginStatus.Success,
                Account = account,
                Profile = profile
            };
        }

        public static UserSessionLoginResult Fail(UserSessionLoginStatus status)
        {
            return new UserSessionLoginResult
            {
                Status = status,
                Account = AccountDto.CreateInvalid(),
                Profile = UserProfileDto.CreateInvalid()
            };
        }
    }
}
