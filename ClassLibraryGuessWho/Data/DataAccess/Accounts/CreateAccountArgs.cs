using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public sealed class CreateAccountArgs
    {
        public string Email { get; set; }
        public byte[] Password { get; set; }
        public string DisplayName { get; set; }

        public DateTime CreationDate { get; set; }
    }
}
