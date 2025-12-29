using System;
using WcfServiceLibraryGuessWho.Communication.Email.Builders.Context;

namespace WcfServiceLibraryGuessWho.Communication.Email.Builders
{
    public sealed class VerificationCodeEmailBuilder : IEmailMessageBuilder<VerificationCodeEmailContext>
    {
        private const string SUBJECT = "Your verification coe";
        private const bool IS_BODY_HTML = false;

        public EmailMessage Build(VerificationCodeEmailContext context)
        {
            if (context == null)
            {
                return new EmailMessage(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    false);
            }

            string body = string.Format(
                "Your verification code is: {0}{1} expires in {2} minutes.",
                context.Code, 
                Environment.NewLine, 
                context.ExpirationMinutes);

            return new EmailMessage(context.Recipient, SUBJECT, body, IS_BODY_HTML);
        }
    }
}
