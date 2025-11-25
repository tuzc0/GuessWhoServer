using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters
{
    public sealed class CreateAccountArgs
    {
        public string Email { get; set; }
        public byte[] Password { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreationDate { get; set; }
        public string AvatarId { get; set; }
    }
}
