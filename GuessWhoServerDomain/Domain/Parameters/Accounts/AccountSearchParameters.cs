namespace GuessWhoServerDomain.Domain.Parameters.Accounts
{
    public sealed class AccountSearchParameters
    {
        public long UserId { get; set; }
        public string Email { get; set; }
    }
}
