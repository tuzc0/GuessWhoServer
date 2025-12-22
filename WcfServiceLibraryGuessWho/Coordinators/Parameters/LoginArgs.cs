using System;

namespace WcfServiceLibraryGuessWho.Coordinators.Parameters
{
    public sealed class LoginArgs
    {
        public LoginArgs(string email, string password)
        {
            Email = email ?? string.Empty;
            Password = password ?? string.Empty;
        }

        public string Email { get; }
        public string Password { get; }
    }
}