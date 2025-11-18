using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters
{
    public sealed class LoginAccountArgs
    {
        public string Email { get; set; }
        public DateTime LastLoginUtcDate { get; set; }
    }
}
