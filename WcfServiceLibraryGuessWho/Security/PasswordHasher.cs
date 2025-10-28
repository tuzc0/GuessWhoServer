using System;
using System.Security.Cryptography;

namespace GuessWho.Services.WCF.Security
{
    internal static class PasswordHasher
    {
        private const int SALT_SIZE_IN_BYTES = 32;     
        private const int HASH_SIZE_IN_BYTES = 32;     
        private const int ITERATION_COUNT = 600_000;  
        private const byte CURRENT_VERSION = 1;
        private const byte ALGORITHM_SHA256 = 1;
        private const int VERSION_SIZE_IN_BYTES = 1;
        private const int ALGORITHM_SIZE_IN_BYTES = 1;
        private const int ITERATIONS_SIZE_IN_BYTES = 4;
        private const int SALT_LENGTH_SIZE_IN_BYTES = 1;
        private static readonly int HEADER_SIZE_IN_BYTES =
            VERSION_SIZE_IN_BYTES + ALGORITHM_SIZE_IN_BYTES + ITERATIONS_SIZE_IN_BYTES + SALT_LENGTH_SIZE_IN_BYTES;
        private const int VERSION_INDEX = 0;
        private const int ALGORITHM_INDEX = VERSION_INDEX + VERSION_SIZE_IN_BYTES;
        private const int ITERATIONS_START_INDEX = ALGORITHM_INDEX + ALGORITHM_SIZE_IN_BYTES;
        private const int SALT_LENGTH_INDEX = ITERATIONS_START_INDEX + ITERATIONS_SIZE_IN_BYTES;
        private const int NO_DIFFERENCE = 0;
        private const int SOURCE_START_INDEX = 0;

        public static byte[] HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var salt = new byte[SALT_SIZE_IN_BYTES];
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(salt);
            }

            byte[] hash;
            using (var kdf = new Rfc2898DeriveBytes(password, salt, ITERATION_COUNT, HashAlgorithmName.SHA256))
            {
                hash = kdf.GetBytes(HASH_SIZE_IN_BYTES);
            }

            var result = new byte[HEADER_SIZE_IN_BYTES + salt.Length + hash.Length];
            result[VERSION_INDEX] = CURRENT_VERSION;
            result[ALGORITHM_INDEX] = ALGORITHM_SHA256;

            WriteInt32BigEndian(result, ITERATIONS_START_INDEX, ITERATION_COUNT);
            result[SALT_LENGTH_INDEX] = (byte)salt.Length;

            var saltStart = HEADER_SIZE_IN_BYTES;
            Buffer.BlockCopy(salt, SOURCE_START_INDEX, result, saltStart, salt.Length);

            var hashStart = saltStart + salt.Length;
            Buffer.BlockCopy(hash, SOURCE_START_INDEX, result, hashStart, hash.Length);

            return result;
        }

        public static bool Verify(string password, byte[] storedPasswordHash)
        {
            if (password == null || storedPasswordHash == null)
            {
                return false;
            }

            if (storedPasswordHash.Length < HEADER_SIZE_IN_BYTES + 1 + 1)
            {
                return false;
            }

            var versionByte = storedPasswordHash[VERSION_INDEX];
            
            if (versionByte != CURRENT_VERSION)
            {
                return false;
            }

            var algorithmByte = storedPasswordHash[ALGORITHM_INDEX];

            if (algorithmByte != ALGORITHM_SHA256)
            {
                return false;
            }

            var iterations = ReadInt32BigEndian(storedPasswordHash, ITERATIONS_START_INDEX);

            if (iterations <= 0)
            {
                return false;
            }

            var saltLength = storedPasswordHash[SALT_LENGTH_INDEX];
            var expectedMinLen = HEADER_SIZE_IN_BYTES + saltLength + 1;

            if (saltLength <= 0 || storedPasswordHash.Length < expectedMinLen)
            {
                return false;
            }

            var salt = new byte[saltLength];
            var saltStart = HEADER_SIZE_IN_BYTES;
            Buffer.BlockCopy(storedPasswordHash, saltStart, salt, SOURCE_START_INDEX, saltLength);

            var storedHashLength = storedPasswordHash.Length - (HEADER_SIZE_IN_BYTES + saltLength);

            if (storedHashLength <= 0)
            {
                return false;
            }

            var storedHash = new byte[storedHashLength];
            var hashStart = saltStart + saltLength;
            Buffer.BlockCopy(storedPasswordHash, hashStart, storedHash, SOURCE_START_INDEX, storedHashLength);

            byte[] computedHash;
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                computedHash = kdf.GetBytes(storedHashLength);
            }

            var diff = NO_DIFFERENCE;
            
            for (int i = SOURCE_START_INDEX; i < storedHashLength; i++)
            {
                diff |= computedHash[i] ^ storedHash[i];
            }

            return diff == NO_DIFFERENCE;
        }

        private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset + 0] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private static int ReadInt32BigEndian(byte[] buffer, int offset)
        {
            return (buffer[offset + 0] << 24)
                 | (buffer[offset + 1] << 16)
                 | (buffer[offset + 2] << 8)
                 | (buffer[offset + 3]);
        }
    }
}
