using System;

namespace GuessWhoServerDomain.Domain.Models.Accounts
{
    public sealed class UserProfileSnapshot
    {
        public long UserId { get; }
        public string DisplayName { get; }
        public string AvatarId { get; }
        public bool IsGuest { get; }

        public bool IsValid => UserId > 0;

        public UserProfileSnapshot(long userId, string displayName, string avatarId, bool isGuest)
        {
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            AvatarId = avatarId ?? string.Empty;
            IsGuest = isGuest;
        }

        public static UserProfileSnapshot Invalid() => new UserProfileSnapshot(0, string.Empty, string.Empty, false);
    }
}