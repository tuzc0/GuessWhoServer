using System;

namespace GuessWhoServerDomain.Domain.Models.Accounts
{
    public sealed class ProfileSnapshot
    {
        public long AccountId { get; set; }
        public bool IsEmailVerified { get; set; }

        public string Username { get; }
        public string Email { get; }
        public DateTime CreatedAtUtc { get; }
        public string AvatarId { get; }

        public ProfileSnapshot(long accountId, bool isEmailVerified, string username, string email, DateTime createdAtUtc, string avatarId)
        {
            AccountId = accountId;
            IsEmailVerified = isEmailVerified;
            Username = username ?? string.Empty;
            Email = email ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            AvatarId = avatarId ?? string.Empty;
        }
    }
}
