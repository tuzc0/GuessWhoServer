using GuessWhoServerDomain.Domain.Settings;
using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace ConsoleGuessWho.Infraestructure.Settings
{
    internal static class UserSecuritySettingsLoader
    {
        private const string KEY_CODE_LIFETIME_MINUTES = "Security.VerificationCodeLifetimeMinutes";
        private const string KEY_REGEX_TIMEOUT_MS = "Security.RegexTimeoutMs";
        private const string KEY_MAX_FAILED_ATTEMPTS = "Security.MaxFailedAttempts";
        private const string KEY_CODE_PATTERN = "Security.VerificationCodePattern";
        private const string KEY_RESEND_COOLDOWN_SECONDS = "Security.ResendCooldownSeconds";
        private const string KEY_RESEND_HOURLY_MAX_TOKENS = "Security.ResendHourlyMaxTokens";
        private const string KEY_DUMMY_PASSWORD_HASH_BASE64 = "Security.DummyPasswordHashBase64";

        private const int DEFAULT_CODE_LIFETIME_MINUTES = 10;
        private const int DEFAULT_REGEX_TIMEOUT_MS = 100;
        private const int DEFAULT_MAX_FAILED_ATTEMPTS = 5;
        private const int DEFAULT_RESEND_COOLDOWN_SECONDS = 60;
        private const int DEFAULT_RESEND_HOURLY_MAX_TOKENS = 5;
        private const string DEFAULT_DUMMY_PASSWORD_HASH_BASE64 = "";
        private const string DEFAULT_CODE_PATTERN = "^[0-9]{6}$";

        private const int MIN_CODE_LIFETIME_MINUTES = 1;
        private const int MAX_CODE_LIFETIME_MINUTES = 60;

        private const int MIN_REGEX_TIMEOUT_MS = 50;
        private const int MAX_REGEX_TIMEOUT_MS = 5000;

        private const int MIN_MAX_FAILED_ATTEMPTS = 1;
        private const int MAX_MAX_FAILED_ATTEMPTS = 20;

        private const int MIN_RESEND_COOLDOWN_SECONDS = 10;
        private const int MAX_RESEND_COOLDOWN_SECONDS = 600;

        private const int MIN_RESEND_HOURLY_MAX_TOKENS = 1;
        private const int MAX_RESEND_HOURLY_MAX_TOKENS = 100;

        public static UserSecuritySettings Load()
        {
            var app = ConfigurationManager.AppSettings;

            int lifetimeMinutes = ReadInt(app[KEY_CODE_LIFETIME_MINUTES], DEFAULT_CODE_LIFETIME_MINUTES);
            lifetimeMinutes = ClampInt(lifetimeMinutes, MIN_CODE_LIFETIME_MINUTES, MAX_CODE_LIFETIME_MINUTES);

            int regexTimeoutMs = ReadInt(app[KEY_REGEX_TIMEOUT_MS], DEFAULT_REGEX_TIMEOUT_MS);
            regexTimeoutMs = ClampInt(regexTimeoutMs, MIN_REGEX_TIMEOUT_MS, MAX_REGEX_TIMEOUT_MS);

            int maxFailedAttempts = ReadInt(app[KEY_MAX_FAILED_ATTEMPTS], DEFAULT_MAX_FAILED_ATTEMPTS);
            maxFailedAttempts = ClampInt(maxFailedAttempts, MIN_MAX_FAILED_ATTEMPTS, MAX_MAX_FAILED_ATTEMPTS);

            string patternRaw = (app[KEY_CODE_PATTERN] ?? string.Empty).Trim();
            string patternCandidate = string.IsNullOrWhiteSpace(patternRaw) ? DEFAULT_CODE_PATTERN : patternRaw;

            int resendCooldownSeconds = ReadInt(app[KEY_RESEND_COOLDOWN_SECONDS], DEFAULT_RESEND_COOLDOWN_SECONDS);
            resendCooldownSeconds = ClampInt(resendCooldownSeconds, MIN_RESEND_COOLDOWN_SECONDS, MAX_RESEND_COOLDOWN_SECONDS);

            int resendHourlyMaxTokens = ReadInt(app[KEY_RESEND_HOURLY_MAX_TOKENS], DEFAULT_RESEND_HOURLY_MAX_TOKENS);
            resendHourlyMaxTokens = ClampInt(resendHourlyMaxTokens, MIN_RESEND_HOURLY_MAX_TOKENS, MAX_RESEND_HOURLY_MAX_TOKENS);

            string dummyHashBase64 = (app[KEY_DUMMY_PASSWORD_HASH_BASE64] ?? DEFAULT_DUMMY_PASSWORD_HASH_BASE64).Trim();

            TimeSpan regexTimeout = TimeSpan.FromMilliseconds(regexTimeoutMs);

            string safePattern = TryGetValidRegexPattern(patternCandidate, regexTimeout);

            return new UserSecuritySettings(
                TimeSpan.FromMinutes(lifetimeMinutes),
                regexTimeout,
                maxFailedAttempts,
                safePattern,
                resendCooldownSeconds,
                resendHourlyMaxTokens,
                dummyHashBase64);
        }

        private static string TryGetValidRegexPattern(string patternCandidate, TimeSpan timeout)
        {
            string safeCandidate = (patternCandidate ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(safeCandidate))
            {
                return DEFAULT_CODE_PATTERN;
            }

            try
            {
                _ = new Regex(
                    safeCandidate,
                    RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    timeout);

                return safeCandidate;
            }
            catch (ArgumentException)
            {
                return DEFAULT_CODE_PATTERN;
            }
        }

        private static int ReadInt(string raw, int fallback)
        {
            return int.TryParse(raw, out int value) ? value : fallback;
        }

        private static int ClampInt(int value, int minValue, int maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }

            if (value > maxValue)
            {
                return maxValue;
            }

            return value;
        }
    }
}
