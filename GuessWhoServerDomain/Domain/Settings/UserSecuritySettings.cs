using System;

namespace GuessWhoServerDomain.Domain.Settings
{
    public sealed class UserSecuritySettings
    {
        public TimeSpan VerificationCodeLifetime { get; }
        public TimeSpan RegexTimeout { get; }
        public int MaxFailedAttempts { get; }
        public string VerificationCodePattern { get; }

        public int ResendCooldownSeconds { get; }
        public int ResendHourlyMaxTokens { get; }

        public string DummyPasswordHashBase64 { get; }

        public UserSecuritySettings(
            TimeSpan verificationCodeLifetime,
            TimeSpan regexTimeout,
            int maxFailedAttempts,
            string verificationCodePattern,
            int resendCooldownSeconds,
            int resendHourlyMaxTokens,
            string dummyPasswordHashBase64)
        {
            VerificationCodeLifetime = verificationCodeLifetime;
            RegexTimeout = regexTimeout;
            MaxFailedAttempts = maxFailedAttempts;
            VerificationCodePattern = verificationCodePattern ?? string.Empty;

            ResendCooldownSeconds = resendCooldownSeconds;
            ResendHourlyMaxTokens = resendHourlyMaxTokens;

            DummyPasswordHashBase64 = (dummyPasswordHashBase64 ?? string.Empty).Trim();
        }
    }
}
