using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification
{
    public interface IEmailVerificationManager
    {
        VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request);
        void ResendEmailVerificationCode(ResendVerificationRequest request);
    }
}
