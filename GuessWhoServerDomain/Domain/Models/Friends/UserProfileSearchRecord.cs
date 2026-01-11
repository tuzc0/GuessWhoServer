namespace GuessWhoServerDomain.Domain.Models.Friends
{
    public sealed class UserProfileSearchRecord
    {
        private const long INVALID_ID = 0;
        private const string INVALID_ID_AVATAR = "";

        public long UserId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarId { get; set; }

        public bool IsValid => UserId > INVALID_ID;

        public UserProfileSearchRecord() { }

        public UserProfileSearchRecord(long userId, string displayName, string avatarId)
        {
            UserId = userId;
            DisplayName = displayName ?? string.Empty;
            AvatarId = avatarId;
        }

        public static UserProfileSearchRecord CreateInvalid()
        {
            return new UserProfileSearchRecord
            {
                UserId = INVALID_ID,
                DisplayName = string.Empty,
                AvatarId = INVALID_ID_AVATAR
            };
        }
    }
}