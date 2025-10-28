
namespace WcfServiceLibraryGuessWho.Communication.Email
{
    public interface IVerificationEmailSender
    {
        void SendVerificationCode(string recipientEmailAddress, string verificationCode);
    }
}
