using WcfServiceLibraryGuessWho.Communication.Email.Builders;

namespace WcfServiceLibraryGuessWho.Communication.Email
{
    public interface IEmailSender
    {
        EmailSendResult Send(EmailMessage message);
    }
}
