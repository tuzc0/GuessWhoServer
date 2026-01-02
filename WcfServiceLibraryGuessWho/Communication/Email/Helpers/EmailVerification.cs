using System;
using System.Net.Mail;

namespace GuessWhoServices.Communication.Email.Helpers
{
    internal static class EmailValidation
    {
        internal static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var parsed = new MailAddress(email);

                return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
