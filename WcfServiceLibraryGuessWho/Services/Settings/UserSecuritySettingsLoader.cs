using System;
using System.Configuration;

namespace WcfServiceLibraryGuessWho.Services.Settings
{
    public class UserSecuritySettingsLoader
    {
        private const string KEY_CODE_LIFETIME_MINUTES = "Security.VerificationCodeLifetimeMinutes";
        private const string KEY_REGEX_TIMEOUT_MS = "Security.RegexTimeoutMs";
        private const string KEY_MAX_FAILED_ATTEMPTS = "Security.MaxFailedAttempts";
        private const string KEY_CODE_PATTERN = "Security.VerificationCodePattern";

        private const int DEFAULT_CODE_LIFETIME_MINUTES = 10;
        private const int DEFAULT_REGEX_TIMEOUT_MS = 100;
        private const int DEFAULT_MAX_FAILED_ATTEMPTS = 5;
        private const string DEFAULT_CODE_PATTERN = "^[0-9]{6}$";

        public static UserSecuritySettings Load()
        {
            var app = ConfigurationManager.AppSettings;

            int lifetimeMinutes = ReadInt(app[KEY_CODE_LIFETIME_MINUTES], DEFAULT_CODE_LIFETIME_MINUTES);
            int regexTimeoutMs = ReadInt(app[KEY_REGEX_TIMEOUT_MS], DEFAULT_REGEX_TIMEOUT_MS);
            int maxFailedAttempts = ReadInt(app[KEY_MAX_FAILED_ATTEMPTS], DEFAULT_MAX_FAILED_ATTEMPTS);

            string pattern = app[KEY_CODE_PATTERN] ?? DEFAULT_CODE_PATTERN;

            return new UserSecuritySettings(
                TimeSpan.FromMinutes(lifetimeMinutes),
                TimeSpan.FromMilliseconds(regexTimeoutMs),
                maxFailedAttempts,
                pattern);
        }

        private static int ReadInt(string raw, int fallback)
        {
            return int.TryParse(raw, out int value) ? value : fallback;
        }
    }
}
