using GuessWhoCore.Contracts.Faults;
using GuessWhoServer.Security;
using GuessWhoServerDomain.Domain.Enums.Security;
using GuessWhoServerDomain.Domain.Interfaces.Security;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Security.Cryptography;

namespace WcfServiceLibraryGuessWho.Coordinators.EmailVerification
{
    public sealed class VerificationCodeService : IVerificationCodeService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(VerificationCodeService));

        public VerificationCodeResult CreateVerificationCodeOrFault()
        {
            try
            {
                string generatedCode = CodeGenerator.GenerateNumericCode();
                byte[] generatedHash = CodeGenerator.ComputeSha256Hash(generatedCode);

                return new VerificationCodeResult(generatedCode, generatedHash);
            }
            catch (ArgumentNullException ex)
            {
                Logger.Error(
                    "Crypto random generator unavailable (ArgumentNullException) while generating verification code.",
                    ex);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    EmailVerificationFaultKeys.MSG_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    EmailVerificationFaultKeys.FALLBACK_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    ex);
            }
            catch (CryptographicException ex)
            {
                Logger.Error(
                    "Crypto random generator unavailable (CryptographicException) while generating verification code.",
                    ex);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    EmailVerificationFaultKeys.MSG_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    EmailVerificationFaultKeys.FALLBACK_CRYPTO_RANDOM_GENERATOR_UNAVAILABLE,
                    ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Error("Verification code generation failed (ArgumentOutOfRangeException).", ex);

                throw FaultsFactory.Create(
                    EmailVerificationFaultKeys.CODE_VERIFICATION_CODE_GENERATION_FAILED,
                    EmailVerificationFaultKeys.MSG_VERIFICATION_CODE_GENERATION_FAILED,
                    EmailVerificationFaultKeys.FALLBACK_VERIFICATION_CODE_GENERATION_FAILED,
                    ex);
            }
        }

        public byte[] ComputeSha256Hash(string verificationCode)
        {
            return CodeGenerator.ComputeSha256Hash(verificationCode);
        }

        public bool AreEqualConstantTime(byte[] firstByteSequence, byte[] secondByteSequence)
        {
            if (firstByteSequence == null || secondByteSequence == null)
            {
                return firstByteSequence == secondByteSequence;
            }

            int accumulatedDifference = firstByteSequence.Length ^ secondByteSequence.Length;
            int maxLength = Math.Max(firstByteSequence.Length, secondByteSequence.Length);

            for (int byteIndex = 0; byteIndex < maxLength; byteIndex++)
            {
                byte firstByte = byteIndex < firstByteSequence.Length ? firstByteSequence[byteIndex] : (byte)0;
                byte secondByte = byteIndex < secondByteSequence.Length ? secondByteSequence[byteIndex] : (byte)0;
                accumulatedDifference |= firstByte ^ secondByte;
            }

            return accumulatedDifference == 0;
        }
    }
}
