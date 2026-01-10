using System;

namespace GuessWhoServerDomain.Domain.Parameters.EmailVerification
{
    public sealed class ConsumeActiveTokensArgs
    {
        public long AccountId { get; set; }
        public DateTime ConsumedUtc { get; set; }
    }
}