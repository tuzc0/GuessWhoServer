namespace GuessWhoCore.Validation.ValidationDTOs
{
    public sealed class PasswordConfirmationDraft
    {
        public PasswordConfirmationDraft(string password, string confirmPassword)
        {
            Password = password ?? string.Empty;
            ConfirmPassword = confirmPassword ?? string.Empty;
        }

        public string Password { get; }
        public string ConfirmPassword { get; }
    }

    public sealed class PasswordChangeDraft
    {
        public PasswordChangeDraft(string currentPassword, string newPassword)
        {
            CurrentPassword = currentPassword ?? string.Empty;
            NewPassword = newPassword ?? string.Empty;
        }

        public string CurrentPassword { get; }
        public string NewPassword { get; }
    }
}
