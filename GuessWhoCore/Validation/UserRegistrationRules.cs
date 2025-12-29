using GuessWhoCore.Validation.ValidationDTOs;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GuessWhoCore.Validation
{
    public static class UserRegistrationRules
    {
        private const int EMAIL_MAX_LENGTH = 254;

        private const int DISPLAY_NAME_MIN_LENGTH = 3;
        private const int DISPLAY_NAME_MAX_LENGTH = 50;

        private const int PASSWORD_MIN_LENGTH = 8;
        private const int PASSWORD_MAX_LENGTH = 64;

        private const int REGEX_VALIDATION_TIMEOUT_MS = 250;

        private static readonly Regex DisplayNameRegex = new Regex(
            @"^[\p{L}\p{N} -]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(REGEX_VALIDATION_TIMEOUT_MS));

        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(REGEX_VALIDATION_TIMEOUT_MS));

        public static IReadOnlyList<ValidationError> Validate(UserRegistrationDraft draft)
        {
            if (draft == null)
            {
                return new[]
                {
                    ValidationError.Create("Registration.InvalidRequest")
                };
            }

            var errors = new List<ValidationError>();

            ValidateEmail(draft.Email, errors);
            ValidateDisplayName(draft.DisplayName, errors);
            ValidatePassword(draft.Password, errors);
            ValidateConfirmPassword(draft.Password, draft.ConfirmPassword, errors);

            return errors;
        }

        private static void ValidateEmail(string email, ICollection<ValidationError> errors)
        {
            email = (email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(ValidationError.Create("Registration.Email.Required"));
                return;
            }

            if (email.Length > EMAIL_MAX_LENGTH)
            {
                errors.Add(ValidationError.Create("Registration.Email.TooLong"));
            }

            if (!EmailRegex.IsMatch(email))
            {
                errors.Add(ValidationError.Create("Registration.Email.InvalidFormat"));
            }
        }

        private static void ValidateDisplayName(string displayName, ICollection<ValidationError> errors)
        {
            displayName = (displayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add(ValidationError.Create("Registration.DisplayName.Required"));
                return;
            }

            if (displayName.Length < DISPLAY_NAME_MIN_LENGTH)
            {
                errors.Add(ValidationError.Create("Registration.DisplayName.TooShort"));
            }

            if (displayName.Length > DISPLAY_NAME_MAX_LENGTH)
            {
                errors.Add(ValidationError.Create("Registration.DisplayName.TooLong"));
            }

            if (!DisplayNameRegex.IsMatch(displayName))
            {
                errors.Add(ValidationError.Create("Registration.DisplayName.InvalidFormat"));
            }
        }

        private static void ValidatePassword(string password, ICollection<ValidationError> errors)
        {
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add(ValidationError.Create("Registration.Password.Required"));
                return;
            }

            if (password.Length < PASSWORD_MIN_LENGTH)
            {
                errors.Add(ValidationError.Create("Registration.Password.TooShort"));
            }

            if (password.Length > PASSWORD_MAX_LENGTH)
            {
                errors.Add(ValidationError.Create("Registration.Password.TooLong"));
            }
        }

        private static void ValidateConfirmPassword(
            string password,
            string confirmPassword,
            ICollection<ValidationError> errors)
        {
            confirmPassword = confirmPassword ?? string.Empty;


            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                errors.Add(ValidationError.Create("Registration.ConfirmPassword.Required"));
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                errors.Add(ValidationError.Create("Registration.ConfirmPassword.Mismatch"));
            }
        }
    }
}
