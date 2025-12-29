using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts
{
    public sealed class UpdatePasswordArgs
    {
        public long AccountId { get; set; }
        public byte[] NewPasswordHash { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
