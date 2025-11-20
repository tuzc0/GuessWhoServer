using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class EmailVerificationResendLimitsDto
    {
        public bool IsPerMinuteCooldownActive { get; set; }
        public bool IsWithinHourlyLimit { get; set; }
        public DateTime? LastTokenCreatedAtUtc { get; set; }
        public int TokensSentInLastHour { get; set; }
    }
}
