namespace GuessWhoServices.Communication.Email.Builders.Context
{
    public sealed class VerificationCodeEmailContext
    {
        public VerificationCodeEmailContext(string recipient, string code, int expirationMinutes) 
        {
            Recipient = recipient ?? string.Empty;
            Code = code ?? string.Empty;
            ExpirationMinutes = expirationMinutes;
        }

        public string Recipient { get; }
        public string Code { get; }
        public int ExpirationMinutes { get; }
    }
}
