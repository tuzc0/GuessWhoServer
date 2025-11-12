using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class UserProfileDto
    {
        public long UserId { get; set; }

        public string DisplayName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
