using GuessWhoContracts.Enums;

namespace GuessWhoContracts.Dtos.Dto
{
    public class UserSessionLoginResult
    {
        public UserSessionLoginStatus Status { get; private set; }
        public AccountDto Account { get; private set; }
        public UserProfileDto Profile { get; private set; }

        public bool IsSuccess => Status == UserSessionLoginStatus.Success;

        private UserSessionLoginResult()
        {
        }

        public static UserSessionLoginResult CreateSuccessful(AccountDto account, UserProfileDto profile)
        {
            return new UserSessionLoginResult
            {
                Status = UserSessionLoginStatus.Success,
                Account = account,
                Profile = profile
            };
        }

        public static UserSessionLoginResult CreateFailed(UserSessionLoginStatus status)
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