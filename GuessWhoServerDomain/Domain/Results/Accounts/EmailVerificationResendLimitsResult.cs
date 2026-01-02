using System;

namespace GuessWhoServerDomain.Domain.Results.Accounts
{
    public sealed class EmailVerificationResendLimitsResult
    {
        public bool IsPerMinuteCooldownActive { get; set; }
        public bool IsWithinHourlyLimit { get; set; }
        public DateTime? LastTokenCreatedAtUtc { get; set; }
        public int TokensSentInLastHour { get; set; }
    }
}
