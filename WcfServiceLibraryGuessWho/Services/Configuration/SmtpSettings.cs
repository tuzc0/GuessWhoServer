using System;

namespace GuessWhoServices.Services.Configuration
{
    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string FromAddress { get; set; }
        public string DisplayName { get; set; }
        public int TimeoutMs { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new InvalidOperationException("SMTP Host is not configured.");
            }
            if (string.IsNullOrWhiteSpace(User))
            {
                throw new InvalidOperationException("SMTP User is not configured.");
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("SMTP Password is not configured.");
            }
        }

        public bool TryValidate(out string missingField)
        {
            missingField = string.Empty;

            if (string.IsNullOrWhiteSpace(Host))
            {
                missingField = nameof(Host);
                return false;
            }

            if (Port <= 0)
            {
                missingField = nameof(Port);
                return false;
            }

            if (string.IsNullOrWhiteSpace(User))
            {
                missingField = nameof(User);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                missingField = nameof(Password);
                return false;
            }

            if (string.IsNullOrWhiteSpace(FromAddress))
            {
                missingField = nameof(FromAddress);
                return false;
            }

            return true;
        }
    }
}
