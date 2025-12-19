using System;

namespace WcfServiceLibraryGuessWho.Coordinators.Parameters
{
    public class RegisterUserArgs
    {
        public RegisterUserArgs(string email, string displayName, string password, DateTime nowUtc)
        {
            Email = email;
            DisplayName = displayName;
            Password = password;
            NowUtc = nowUtc;
        }

        public string Email { get; }
        public string DisplayName { get; }
        public string Password { get; }
        public DateTime NowUtc { get; }
    }
}
