using System;
using System.Security.Cryptography;
using System.Text;

namespace GuessWho.Services.Security
{
    public static class CodeGenerator
    {
        public static string GenerateNumericCode()
        {
            using (var cryptographicRandomNumberGenerator = RandomNumberGenerator.Create())
            {
                int randomNumber = GetInt32Compat(cryptographicRandomNumberGenerator, 0, 1_000_000); 
                return randomNumber.ToString("D6");
            }
        }

        public static byte[] ComputeSha256Hash(string inputText)
        {
            byte[] codeUtf8Bytes = Encoding.UTF8.GetBytes(inputText ?? string.Empty);

            using (var sha256Algorithm = SHA256.Create())
            {
                return sha256Algorithm.ComputeHash(codeUtf8Bytes);
            }
        }

        private static int GetInt32Compat(RandomNumberGenerator randomNumberGenerator, int minInclusive, int maxExclusive)
        {
            if (randomNumberGenerator == null)
            {
                throw new ArgumentNullException(nameof(randomNumberGenerator));
            }

            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            uint range = (uint)(maxExclusive - minInclusive);
            var buffer = new byte[4];

            uint limit = (uint.MaxValue / range) * range;
            uint value;

            do
            {
                randomNumberGenerator.GetBytes(buffer);
                value = BitConverter.ToUInt32(buffer, 0);

            } while (value >= limit);

            return (int)(minInclusive + (value % range));
        }
    }

    public sealed class VerificationCodeResult
    {
        public string PlainCode { get; }
        public byte[] HashCode { get; }

        public VerificationCodeResult(string plainCode, byte[] hashCode)
        {
            PlainCode = plainCode ?? throw new ArgumentNullException(nameof(plainCode));
            HashCode = hashCode ?? throw new ArgumentNullException(nameof(hashCode));
        }
    }

}
