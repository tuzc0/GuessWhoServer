using System;

namespace GuessWhoServerDomain.Domain.Models.Accounts
{
    public sealed class UpdatedProfileSnapshot
    {
        public bool Updated { get; }
        public string Username { get; }
        public string Email { get; }
        public DateTime UpdatedAtUtc { get; }
        public string AvatarId { get; }

        public UpdatedProfileSnapshot(bool updated, string username, string email, DateTime updatedAtUtc, string avatarId)
        {
            Updated = updated;
            Username = username ?? string.Empty;
            Email = email ?? string.Empty;
            UpdatedAtUtc = updatedAtUtc;
            AvatarId = avatarId ?? string.Empty;
        }
    }
}
