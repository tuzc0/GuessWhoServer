using GuessWhoCore.Validation.ValidationDTOs;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GuessWhoCore.Validation
{
    public static class UserRules
    {
        private const int EMAIL_MAX_LENGTH = 254;

        private const int DISPLAY_NAME_MIN_LENGTH = 3;
        private const int DISPLAY_NAME_MAX_LENGTH = 50;

        private const int PASSWORD_MIN_LENGTH = 8;
        private const int PASSWORD_MAX_LENGTH = 64;

        private const int AVATAR_ID_MAX_LENGTH = 16; 

        private const int REGEX_VALIDATION_TIMEOUT_MS = 250;

        private static readonly Regex DisplayNameRegex = new Regex(
            @"^[\p{L}\p{N} -]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(REGEX_VALIDATION_TIMEOUT_MS));

        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(REGEX_VALIDATION_TIMEOUT_MS));

        public static IReadOnlyList<ValidationError> Validate(UserRulesDraft draft)
        {
            if (draft == null)
            {
                return new[] { ValidationError.Create("Registration.InvalidRequest") };
            }

            var errors = new List<ValidationError>();

            ValidateEmail(draft.Email, errors);
            ValidateDisplayNameRequired(draft.DisplayName, errors);
            ValidatePasswordRequired(draft.Passwords.Password, errors);
            ValidateConfirmPassword(draft.Passwords, errors);

            return errors;
        }

        public static IReadOnlyList<ValidationError> Validate(ProfileUpdateDraft draft)
        {
            if (draft == null)
            {
                return new[] { ValidationError.Create("Profile.InvalidRequest") };
            }

            var errors = new List<ValidationError>();

            ValidateDisplayNameIfProvided(draft.DisplayName, errors);
            ValidateAvatarIdIfProvided(draft.AvatarId, errors);
            ValidatePasswordChangeIfProvided(draft.PasswordChange, errors);

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

        private static void ValidateDisplayNameRequired(string displayName, ICollection<ValidationError> errors)
        {
            displayName = (displayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add(ValidationError.Create("Registration.DisplayName.Required"));
                return;
            }

            ValidateDisplayNameContent(displayName, "Registration.DisplayName", errors);
        }

        private static void ValidateDisplayNameIfProvided(string displayName, ICollection<ValidationError> errors)
        {
            displayName = (displayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            ValidateDisplayNameContent(displayName, "Profile.DisplayName", errors);
        }

        private static void ValidateDisplayNameContent(string displayName, string keyPrefix, ICollection<ValidationError> errors)
        {
            if (displayName.Length < DISPLAY_NAME_MIN_LENGTH)
            {
                errors.Add(ValidationError.Create(keyPrefix + ".TooShort"));
            }

            if (displayName.Length > DISPLAY_NAME_MAX_LENGTH)
            {
                errors.Add(ValidationError.Create(keyPrefix + ".TooLong"));
            }

            if (!DisplayNameRegex.IsMatch(displayName))
            {
                errors.Add(ValidationError.Create(keyPrefix + ".InvalidFormat"));
            }
        }

        private static void ValidatePasswordRequired(string password, ICollection<ValidationError> errors)
        {
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add(ValidationError.Create("Registration.Password.Required"));
                return;
            }

            ValidatePasswordLength(password, "Registration.Password", errors);
        }

        private static void ValidateConfirmPassword(PasswordConfirmationDraft passwords, ICollection<ValidationError> errors)
        {
            if (passwords == null)
            {
                errors.Add(ValidationError.Create("Registration.ConfirmPassword.Required"));
                return;
            }

            string confirmPassword = passwords.ConfirmPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                errors.Add(ValidationError.Create("Registration.ConfirmPassword.Required"));
                return;
            }

            if (!string.Equals(passwords.Password ?? string.Empty, confirmPassword, StringComparison.Ordinal))
            {
                errors.Add(ValidationError.Create("Registration.ConfirmPassword.Mismatch"));
            }
        }

        private static void ValidateAvatarIdIfProvided(string avatarId, ICollection<ValidationError> errors)
        {
            avatarId = (avatarId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return;
            }

            if (avatarId.Length > AVATAR_ID_MAX_LENGTH)
            {
                errors.Add(ValidationError.Create("Profile.Avatar.TooLong"));
            }
        }

        private static void ValidatePasswordChangeIfProvided(PasswordChangeDraft draft, ICollection<ValidationError> errors)
        {
            if (draft == null)
            {
                return;
            }

            string newPassword = draft.NewPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return;
            }

            string currentPassword = draft.CurrentPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                errors.Add(ValidationError.Create("Profile.Password.CurrentRequired"));
                return;
            }

            ValidatePasswordLength(newPassword, "Profile.Password", errors);
        }

        private static void ValidatePasswordLength(string password, string keyPrefix, ICollection<ValidationError> errors)
        {
            if (password.Length < PASSWORD_MIN_LENGTH)
            {
                errors.Add(ValidationError.Create(keyPrefix + ".TooShort"));
            }

            if (password.Length > PASSWORD_MAX_LENGTH)
            {
                errors.Add(ValidationError.Create(keyPrefix + ".TooLong"));
            }
        }
    }
}
