using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class EmailVerificationTokenDto
    {
        public static readonly Guid INVALID_TOKEN_ID = Guid.Empty;
        public const long INVALID_ACCOUNT_ID = -1;

        public Guid TokenId { get; set; }
        public long AccountId { get; set; }
        public byte[] CodeHash { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public DateTime? ConsumedUtc { get; set; }

        public bool IsValid => TokenId != INVALID_TOKEN_ID;

        public static EmailVerificationTokenDto CreateInvalid()
        {
            return new EmailVerificationTokenDto
            {
                TokenId = INVALID_TOKEN_ID,
                AccountId = INVALID_ACCOUNT_ID,
                CodeHash = Array.Empty<byte>(),
                CreatedAtUtc = DateTime.MinValue,
                ExpiresUtc = DateTime.MinValue,
                ConsumedUtc = null
            };
        }
    }
}
