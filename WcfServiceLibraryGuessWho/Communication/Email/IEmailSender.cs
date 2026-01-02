using GuessWhoServices.Communication.Email.Builders;

namespace GuessWhoServices.Communication.Email
{
    public interface IEmailSender
    {
        EmailSendResult Send(EmailMessage message);
    }
}
