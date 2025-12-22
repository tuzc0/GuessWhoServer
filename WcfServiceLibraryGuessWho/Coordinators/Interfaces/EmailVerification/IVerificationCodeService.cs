using GuessWho.Services.Security;

namespace WcfServiceLibraryGuessWho.Coordinators.Interfaces.EmailVerification
{
    public interface IVerificationCodeService
    {
        VerificationCodeResult CreateVerificationCodeOrFault();
        byte[] ComputeSha256Hash(string verificationCode);
        bool AreEqualConstantTime(byte [] firstByteSequence, byte[] secondByteSequence);
    }
}
