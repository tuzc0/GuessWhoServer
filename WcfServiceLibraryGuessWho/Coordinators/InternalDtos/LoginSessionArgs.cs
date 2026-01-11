using System;
using System.ServiceModel;

namespace GuessWhoServices.Coordinators.InternalDtos
{
    public sealed class LoginSessionArgs
    {
        public LoginSessionArgs(LoginArgs loginArgs, IContextChannel channel)
        {
            LoginArgs = loginArgs ?? throw new ArgumentNullException(nameof(loginArgs));
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public LoginArgs LoginArgs { get; }

        public IContextChannel Channel { get; }
    }

    public sealed class LogoutSessionArgs
    {
        public LogoutSessionArgs(long userProfileId, IContextChannel channel)
        {
            UserProfileId = userProfileId;
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public long UserProfileId { get; }

        public IContextChannel Channel { get; }
    }
}
