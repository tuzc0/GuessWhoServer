using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class AccountDto
    {
        public const long INVALID_ACCOUNT_ID = -1;
        public const long INVALID_USER_ID = -1;

        public long AccountId { get; set; }

        public long UserId { get; set; }

        public string Email { get; set; }

        public byte[] PasswordHash { get; set; }

        public bool IsEmailVerified { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? LastLoginUtc { get; set; }

        public bool IsValid => AccountId != INVALID_ACCOUNT_ID;
        public static AccountDto CreateInvalid()
        {
            return new AccountDto
            {
                AccountId = INVALID_ACCOUNT_ID,
                UserId = INVALID_USER_ID,
                Email = string.Empty,
                PasswordHash = Array.Empty<byte>(),
                IsEmailVerified = false,
                CreatedAtUtc = DateTime.MinValue,
                UpdatedAtUtc = DateTime.MinValue,
                LastLoginUtc = null
            };
        }
    }
}
