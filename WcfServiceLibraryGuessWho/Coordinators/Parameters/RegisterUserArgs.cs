using System;

namespace WcfServiceLibraryGuessWho.Coordinators.Parameters
{
    public sealed class RegisterUserArgs
    {
        public RegisterUserArgs(string email, string displayName, string password, DateTime nowUtc)
        {
            Email = email ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Password = password ?? string.Empty;
            NowUtc = nowUtc;
        }

        public string Email { get; }
        public string DisplayName { get; }
        public string Password { get; }
        public DateTime NowUtc { get; }
    }
}
