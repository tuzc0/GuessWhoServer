using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts.Email
{
    public sealed class ResendLimitsQuery
    {
        public long AccountId { get; }
        public DateTime DateUtc { get; }
        public int CooldownSeconds { get; }
        public int HourlyMaxTokens { get; }

        public ResendLimitsQuery(long accountId, DateTime dateUtc, int cooldownSeconds, int hourlyMaxTokens)
        {
            AccountId = accountId;
            DateUtc = dateUtc;
            CooldownSeconds = cooldownSeconds;
            HourlyMaxTokens = hourlyMaxTokens;
        }
    }

}
