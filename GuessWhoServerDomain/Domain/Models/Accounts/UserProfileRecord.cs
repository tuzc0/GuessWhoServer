using System;

namespace GuessWhoServerDomain.Domain.Models.Accounts
{
    public class UserProfileRecord
    {
        public const long INVALID_USER_ID = -1;
        public const string INVALID_AVATAR_ID = "";

        public long UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsGuest { get; set; } // Añadir esta línea
        public DateTime CreatedAtUtc { get; set; }
        public string AvatarId { get; set; }

        public bool IsValid => UserId != INVALID_USER_ID;

        public static UserProfileRecord CreateInvalid()
        {
            return new UserProfileRecord
            {
                UserId = INVALID_USER_ID,
                DisplayName = string.Empty,
                IsActive = false,
                IsGuest = false, 
                CreatedAtUtc = DateTime.MinValue,
                AvatarId = INVALID_AVATAR_ID
            };
        }
    }
}