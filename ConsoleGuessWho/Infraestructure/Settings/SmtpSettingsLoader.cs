using GuessWhoServices.Services.Configuration;
using System;
using System.Configuration;

namespace ConsoleGuessWho.Infraestructure.Settings
{
    internal static class SmtpSettingsLoader
    {
        private const string KEY_SMTP_HOST = "Smtp.Host";
        private const string KEY_SMTP_PORT = "Smtp.Port";
        private const string KEY_SMTP_ENABLE_SSL = "Smtp.EnableSsl";

        private const string KEY_SMTP_USER = "Smtp.User";
        private const string KEY_SMTP_USERNAME = "Smtp.Username";
        private const string KEY_SMTP_PASSWORD = "Smtp.Password";
        private const string KEY_SMTP_FROM_ADDRESS = "Smtp.FromAddress";

        private const string KEY_SMTP_DISPLAY_NAME = "Smtp.DisplayName";
        private const string KEY_SMTP_TIMEOUT_MS = "Smtp.TimeoutMs";

        private const int DEFAULT_SMTP_PORT = 587;
        private const bool DEFAULT_ENABLE_SSL = true;

        private const string DEFAULT_DISPLAY_NAME = "GuessWho";
        private const int DEFAULT_TIMEOUT_MS = 30000;

        private const int MIN_PORT = 1;
        private const int MAX_PORT = 65535;

        private const int MIN_TIMEOUT_MS = 1000;
        private const int MAX_TIMEOUT_MS = 300000;

        private const string ERROR_MISSING_APPSETTING_FORMAT = "Missing required appSetting: {0}";
        private const string ERROR_INVALID_INT_APPSETTING_FORMAT = "Invalid integer value for appSetting: {0}";

        public static SmtpSettings Load()
        {
            var smtpSettings = new SmtpSettings
            {
                Host = ReadRequiredString(KEY_SMTP_HOST),
                Port = ReadIntOrDefault(new IntSetting(KEY_SMTP_PORT, DEFAULT_SMTP_PORT, MIN_PORT, MAX_PORT)),
                EnableSsl = ReadBoolOrDefault(KEY_SMTP_ENABLE_SSL, DEFAULT_ENABLE_SSL),

                User = ReadRequiredFirstNonEmptyString(new[] { KEY_SMTP_USER, KEY_SMTP_USERNAME }),
                Password = ReadRequiredString(KEY_SMTP_PASSWORD),
                FromAddress = ReadRequiredString(KEY_SMTP_FROM_ADDRESS),

                DisplayName = ReadStringOrDefault(KEY_SMTP_DISPLAY_NAME, DEFAULT_DISPLAY_NAME),
                TimeoutMs = ReadIntOrDefault(new IntSetting(KEY_SMTP_TIMEOUT_MS, DEFAULT_TIMEOUT_MS, MIN_TIMEOUT_MS, MAX_TIMEOUT_MS))
            };

            if (!smtpSettings.TryValidate(out string missingField))
            {
                throw new ConfigurationErrorsException(string.Format(ERROR_MISSING_APPSETTING_FORMAT, missingField));
            }

            return smtpSettings;
        }

        private static string ReadRequiredString(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(string.Format(ERROR_MISSING_APPSETTING_FORMAT, key));
            }

            return value;
        }

        private static string ReadRequiredFirstNonEmptyString(string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                throw new ArgumentException("Keys cannot be null or empty.", nameof(keys));
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string value = ConfigurationManager.AppSettings[keys[i]];

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            throw new ConfigurationErrorsException(string.Format(ERROR_MISSING_APPSETTING_FORMAT, keys[0]));
        }

        private static string ReadStringOrDefault(string key, string defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static bool ReadBoolOrDefault(string key, bool defaultValue)
        {
            string raw = ConfigurationManager.AppSettings[key];
            return bool.TryParse(raw, out bool parsed) ? parsed : defaultValue;
        }

        private static int ReadIntOrDefault(IntSetting setting)
        {
            if (setting == null)
            {
                throw new ArgumentNullException(nameof(setting));
            }

            string raw = ConfigurationManager.AppSettings[setting.Key];

            if (string.IsNullOrWhiteSpace(raw))
            {
                return setting.DefaultValue;
            }

            if (!int.TryParse(raw, out int parsed))
            {
                throw new ConfigurationErrorsException(string.Format(ERROR_INVALID_INT_APPSETTING_FORMAT, setting.Key));
            }

            if (parsed < setting.MinValue || parsed > setting.MaxValue)
            {
                return setting.DefaultValue;
            }

            return parsed;
        }

        private sealed class IntSetting
        {
            public IntSetting(string key, int defaultValue, int minValue, int maxValue)
            {
                Key = string.IsNullOrWhiteSpace(key)
                    ? throw new ArgumentException("Key is required.", nameof(key))
                    : key;

                DefaultValue = defaultValue;
                MinValue = minValue;
                MaxValue = maxValue;
            }

            public string Key { get; }
            public int DefaultValue { get; }
            public int MinValue { get; }
            public int MaxValue { get; }
        }
    }
}
