namespace GuessWhoCore.Validation.ValidationDTOs
{
    public sealed class UserRulesDraft
    {
        public UserRulesDraft(string email, string displayName, PasswordConfirmationDraft passwords)
        {
            Email = email ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Passwords = passwords ?? new PasswordConfirmationDraft(string.Empty, string.Empty);
        }

        public string Email { get; }
        public string DisplayName { get; }
        public PasswordConfirmationDraft Passwords { get; }
    }

    public sealed class ProfileUpdateDraft
    {
        public ProfileUpdateDraft(string displayName, string avatarId, PasswordChangeDraft passwordChange)
        {
            DisplayName = displayName ?? string.Empty;
            AvatarId = avatarId ?? string.Empty;
            PasswordChange = passwordChange ?? new PasswordChangeDraft(string.Empty, string.Empty);
        }

        public string DisplayName { get; }
        public string AvatarId { get; }
        public PasswordChangeDraft PasswordChange { get; }
    }
}
