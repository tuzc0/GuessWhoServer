using GuessWhoServerDomain.Domain.Enums.Security;

namespace GuessWhoServerDomain.Domain.Interfaces.Security
{
    public interface IVerificationCodeService
    {
        VerificationCodeResult CreateVerificationCodeOrFault();
        byte[] ComputeSha256Hash(string verificationCode);
        bool AreEqualConstantTime(byte[] firstByteSequence, byte[] secondByteSequence);
    }
}
