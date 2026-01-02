using GuessWhoServices.Communication.Email.Builders.Context;

namespace GuessWhoServices.Communication.Email.Builders
{
    public sealed class MatchInvitationEmailBuilder : IEmailMessageBuilder<MatchInvitationEmailContext>
    {
        private const string SUBJECT = "Match invitation";
        private const bool IS_BODY_HTML = false;

        public EmailMessage Build(MatchInvitationEmailContext context)
        {
            if (context == null)
            {
                return new EmailMessage(string.Empty, string.Empty, string.Empty, false);
            }

            string body = string.Format(
                "{0} invited you to a match. Use this code to join: {1}",
                context.InviterDisplayName,
                context.MatchCode);

            return new EmailMessage(context.Recipient, SUBJECT, body, IS_BODY_HTML);
        }
    }
}
