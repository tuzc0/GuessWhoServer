namespace GuessWhoCore.Validation.ValidationDTOs
{
    public sealed class LoginDraft
    {
        public LoginDraft(string email, string password)
        {
            Email = email ?? string.Empty;
            Password = password ?? string.Empty;
        }

        public string Email { get; }
        public string Password { get; }
    }
}
