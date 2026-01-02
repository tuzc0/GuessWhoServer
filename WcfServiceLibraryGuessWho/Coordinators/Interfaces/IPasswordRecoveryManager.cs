using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;

namespace GuessWhoServices.Coordinators.Interfaces
{
    public interface IPasswordRecoveryManager
    {
        PasswordRecoveryResponse SendRecoveryPassword(PasswordRecoveryRequest request);
        bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request);
    }
}
