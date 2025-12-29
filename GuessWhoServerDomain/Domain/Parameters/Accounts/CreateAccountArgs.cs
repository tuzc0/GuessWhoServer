using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts
{
    public sealed class CreateAccountArgs
    {
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreationDate { get; set; }
        public string AvatarId { get; set; }
    }
}
