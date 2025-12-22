namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification
{
    public interface IVerificationEmailDispatcher
    {
        void SendVerificationEmailOrThrow(string recipientEmailAddress, string verificationCode);
    }
}
