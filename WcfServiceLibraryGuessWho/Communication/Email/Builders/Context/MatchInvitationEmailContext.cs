namespace GuessWhoServices.Communication.Email.Builders.Context
{
    public sealed class MatchInvitationEmailContext
    {
        public MatchInvitationEmailContext(string recipient, string inviterDisplayNmae, string matchCode) 
        {
            Recipient = recipient ?? string.Empty;
            InviterDisplayName = inviterDisplayNmae ?? string.Empty;
            MatchCode = matchCode ?? string.Empty;

        }

        public string Recipient { get; }
        public string InviterDisplayName { get; }
        public string MatchCode { get; }
    }
}
