namespace GuessWhoServerDomain.Domain.Models.Friends
{
    public sealed class UserProfileSearchRecord
    {
        private const long INVALID_ID = 0;
        private const string INVALID_ID_AVATAR = "";

        public long UserId { get; }
        public string DisplayName { get; }
        public string AvatarId { get; }

        public bool IsValid => UserId > INVALID_ID;

        public UserProfileSearchRecord(long userId, string displayName, string avatarId)
        {
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            AvatarId = avatarId;
        }

        public static UserProfileSearchRecord CreateInvalid()
        {
            return new UserProfileSearchRecord(
                INVALID_ID,
                string.Empty,
                INVALID_ID_AVATAR);
        }
    }
}
