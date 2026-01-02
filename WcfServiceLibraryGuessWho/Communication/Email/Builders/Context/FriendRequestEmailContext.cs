namespace GuessWhoServices.Communication.Email.Builders.Context
{
    public sealed class FriendRequestEmailContext
    {
        public FriendRequestEmailContext(string recipient, string requesterDisplayName) 
        {
            Recipient = recipient ?? string.Empty;
            RequesterDisplayName = requesterDisplayName ?? string.Empty;
        }

        public string Recipient { get; }
        public string RequesterDisplayName { get; }
    }
}
