using GuessWhoContracts.Dtos.RequestAndResponse;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces
{
    public interface IPasswordRecoveryManager
    {
        PasswordRecoveryResponse SendRecoveryPassword(PasswordRecoveryRequest request);
        bool UpdatePasswordWithVerificationCode(UpdatePasswordRequest request);
    }
}
