using System;

namespace WcfServiceLibraryGuessWho.Services.Settings
{
    public class UserSecuritySettings
    {
        public TimeSpan VerificationCodeLifetime { get; }
        public TimeSpan RegexTimeout { get; }
        public int MaxFailedAttempts { get; }

        public string VerificationCodePattern { get; }

        public UserSecuritySettings(
            TimeSpan verificationCodeLifetime, 
            TimeSpan regexTimeout, 
            int maxFailedAttempts,
            string verificationCodePattern)
        {
            VerificationCodeLifetime = verificationCodeLifetime;
            RegexTimeout = regexTimeout;
            MaxFailedAttempts = maxFailedAttempts;
            VerificationCodePattern = verificationCodePattern ?? string.Empty;
        }
    }
}
