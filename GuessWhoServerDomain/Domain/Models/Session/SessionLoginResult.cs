using GuessWhoServerDomain.Domain.Enums.Accounts;
using GuessWhoServerDomain.Domain.Models.Accounts;
using System;

namespace GuessWhoServerDomain.Domain.Models.Sessions
{
    public sealed class SessionLoginResult
    {
        public LoginStatus Status { get; }
        public AccountRecord Account { get; }
        public UserProfileRecord Profile { get; }

        public bool IsSuccess => Status == LoginStatus.Success;

        private SessionLoginResult(
            LoginStatus status,
            AccountRecord account,
            UserProfileRecord profile)
        {
            Status = status;
            Account = account ?? AccountRecord.CreateInvalid();
            Profile = profile ?? UserProfileRecord.CreateInvalid();
        }

        public static SessionLoginResult CreateSuccessful(AccountRecord account, UserProfileRecord profile)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return new SessionLoginResult(
                LoginStatus.Success,
                account,
                profile);
        }

        public static SessionLoginResult CreateFailed(LoginStatus status)
        {
            return new SessionLoginResult(
                status,
                AccountRecord.CreateInvalid(),
                UserProfileRecord.CreateInvalid());
        }
    }
}
