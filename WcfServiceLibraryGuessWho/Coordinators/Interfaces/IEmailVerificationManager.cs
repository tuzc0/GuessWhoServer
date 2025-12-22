
using GuessWhoContracts.Dtos.RequestAndResponse;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification
{
    public interface IEmailVerificationManager
    {
        VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request);
        void ResendEmailVerificationCode(ResendVerificationRequest request);
    }
}
