using System;

namespace GuessWhoServerDomain.Domain.Models.Accounts
{
    public class AccountRecord
    {
        public const long INVALID_ACCOUNT_ID = -1;
        public const long INVALID_USER_ID = -1;
        public const int INVALID_FAILED_LOGINS = -1;

        public long AccountId { get; set; }
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public bool IsEmailVerified { get; set; }
        public bool IsDeleted { get; set; }

        public int FailedLogInUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? LastLoginUtc { get; set; }
        public DateTime? LockedUntilUtc { get; set; }

        public bool IsValid => AccountId != INVALID_ACCOUNT_ID;

        public static AccountRecord CreateInvalid()
        {
            return new AccountRecord
            {
                AccountId = INVALID_ACCOUNT_ID,
                UserId = INVALID_USER_ID,
                Email = string.Empty,
                PasswordHash = Array.Empty<byte>(),
                IsEmailVerified = false,
                IsDeleted = false,
                FailedLogInUtc = INVALID_FAILED_LOGINS,
                CreatedAtUtc = DateTime.MinValue,
                UpdatedAtUtc = DateTime.MinValue,
                LastLoginUtc = null,
                LockedUntilUtc = null
            };
        }
    }
}
