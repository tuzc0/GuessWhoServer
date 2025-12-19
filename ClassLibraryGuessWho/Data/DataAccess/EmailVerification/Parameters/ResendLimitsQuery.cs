using System;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters
{
    public sealed class ResendLimitsQuery
    {
        public long AccountId { get; }
        public DateTime DateUtc { get; }

        public ResendLimitsQuery(long accountId, DateTime dateUtc)
        {
            AccountId = accountId;
            DateUtc = dateUtc;
        }
    }
}
