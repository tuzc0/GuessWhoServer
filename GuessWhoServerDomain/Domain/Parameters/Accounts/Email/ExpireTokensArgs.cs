using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts.Email
{
    public sealed class ExpireTokensArgs
    {
        public long AccountId { get; }
        public DateTime NewExpirationUtc { get; }

        public ExpireTokensArgs(long accountId, DateTime newExpirationUtc)
        {
            AccountId = accountId;
            NewExpirationUtc = newExpirationUtc;
        }
    }
}
