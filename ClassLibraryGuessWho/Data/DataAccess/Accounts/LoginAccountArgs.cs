using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public sealed class LoginAccountArgs
    {
        public string Email { get; set; }
        public DateTime LastLoginUtcDate { get; set; }
    }
}
