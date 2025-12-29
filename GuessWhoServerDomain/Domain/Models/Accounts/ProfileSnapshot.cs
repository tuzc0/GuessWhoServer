using System;

namespace GuessWhoServerDomain.Domain.Models.Accounts
{
    public sealed class ProfileSnapshot
    {
        public string Username { get; }
        public string Email { get; }
        public DateTime CreatedAtUtc { get; }
        public string AvatarId { get; }

        public ProfileSnapshot(string username, string email, DateTime createdAtUtc, string avatarId)
        {
            Username = username ?? string.Empty;
            Email = email ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            AvatarId = avatarId ?? string.Empty;
        }
    }
}
