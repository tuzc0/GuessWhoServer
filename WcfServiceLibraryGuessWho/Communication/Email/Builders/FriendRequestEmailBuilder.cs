using WcfServiceLibraryGuessWho.Communication.Email.Builders.Context;

namespace WcfServiceLibraryGuessWho.Communication.Email.Builders
{
    public sealed class FriendRequestEmailBuilder : IEmailMessageBuilder<FriendRequestEmailContext>
    {
        private const string SUBJECT = "New friend request";
        private const bool IS_BODY_HTML = false;

        public EmailMessage Build(FriendRequestEmailContext context)
        {
            if (context == null)
            {
                return new EmailMessage(string.Empty, string.Empty, string.Empty, false);
            }

            string body = string.Format(
                "{0} sent you a friend request.",
                context.RequesterDisplayName);

            return new EmailMessage(context.Recipient, SUBJECT, body, IS_BODY_HTML);
        }
    }
}
