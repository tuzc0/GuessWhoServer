using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IEmailVerificationManager
    {
        VerifyEmailResponse ConfirmEmailAddressWithVerificationCode(VerifyEmailRequest request);
        void ResendEmailVerificationCode(ResendVerificationRequest request);
    }
}
