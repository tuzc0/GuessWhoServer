using System;

namespace GuessWhoCore.Validation.ValidationDTOs
{
    [Obsolete("Use UserRulesDraft.")]
    public class UserRegistrationDraft 
    {
        public UserRegistrationDraft(string email, string displayName, string password, string confirmPassword)
        {
            Email = email ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Password = password ?? string.Empty;
            ConfirmPassword = confirmPassword ?? string.Empty;
        } 

        public string Email { get; }
        public string DisplayName { get; }
        public string Password { get; }
        public string ConfirmPassword { get; }
    }
}
