using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class UserProfileDto
    {
        private const long INVALID_USER_ID = -1;

        public long UserId { get; set; }

        public string DisplayName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string AvatarId { get; set; }

        public bool IsValid => UserId != INVALID_USER_ID;

        public static UserProfileDto CreateInvalid()
        {
            return new UserProfileDto
            {
                UserId = INVALID_USER_ID,
                DisplayName = string.Empty,
                IsActive = false,
                CreatedAtUtc = DateTime.MinValue,
                AvatarId = string.Empty
            };
        }
    }
}
