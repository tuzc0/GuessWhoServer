using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts.Email
{
    public sealed class IncrementFailedAttemptArgs
    {
        public Guid TokenId { get; }
        public DateTime NowUtc { get; }
        public int MaxAttempts { get; }

        public IncrementFailedAttemptArgs(Guid tokenId, DateTime nowUtc, int maxAttempts)
        {
            TokenId = tokenId;
            NowUtc = nowUtc;
            MaxAttempts = maxAttempts;
        }
    }
}
