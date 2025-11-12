using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class AccountDto
    {
        public long AccountId { get; set; }

        public long UserId { get; set; }

        public string Email { get; set; }

        public byte[] PasswordHash { get; set; }

        public bool IsEmailVerified { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? LastLoginUtc { get; set; }
    }
}
