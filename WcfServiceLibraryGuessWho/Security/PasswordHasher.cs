using System;
using System.Security.Cryptography;

namespace GuessWho.Services.WCF.Security
{
    internal static class PasswordHasher
    {

        private const int SALT_SIZE_IN_BYTES = 16;
        private const int HASH_SIZE_IN_BYTES = 32;
        private const int ITERATION_COUNT = 100000;
        private const byte CURRENT_VERSION = 1;

        private const int VERSION_SIZE_IN_BYTES = 1;
        private static readonly int TOTAL_HASH_LENGTH = VERSION_SIZE_IN_BYTES + SALT_SIZE_IN_BYTES + HASH_SIZE_IN_BYTES;
        private const int VERSION_INDEX = 0;
        private static readonly int SALT_START_INDEX = VERSION_SIZE_IN_BYTES;
        private static readonly int HASH_START_INDEX = VERSION_SIZE_IN_BYTES + SALT_SIZE_IN_BYTES;

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

            using (var passwordDerivationFunction = new Rfc2898DeriveBytes(password, salt, ITERATION_COUNT))
            {
                var hash = passwordDerivationFunction.GetBytes(HASH_SIZE_IN_BYTES);

                var hashedPasswordResult = new byte[TOTAL_HASH_LENGTH];
                hashedPasswordResult[VERSION_INDEX] = CURRENT_VERSION;

                Buffer.BlockCopy(salt, SOURCE_START_INDEX, hashedPasswordResult, SALT_START_INDEX, salt.Length);
                Buffer.BlockCopy(hash, SOURCE_START_INDEX, hashedPasswordResult, HASH_START_INDEX, hash.Length);

                return hashedPasswordResult;
            }
        }

        public static bool Verify(string password, byte[] storedPasswordHash)
        {
            if (storedPasswordHash == null || storedPasswordHash.Length != TOTAL_HASH_LENGTH)
            {
                return false;
            }

            var versionByte = storedPasswordHash[VERSION_INDEX];

            if (versionByte != CURRENT_VERSION)
            {
                return false;
            }

            var salt = new byte[SALT_SIZE_IN_BYTES];
            var storedHash = new byte[HASH_SIZE_IN_BYTES];

            Buffer.BlockCopy(storedPasswordHash, SALT_START_INDEX, salt, SOURCE_START_INDEX, SALT_SIZE_IN_BYTES);
            Buffer.BlockCopy(storedPasswordHash, HASH_START_INDEX, storedHash, SOURCE_START_INDEX, HASH_SIZE_IN_BYTES);

            using (var passwordDerivationFunction = new Rfc2898DeriveBytes(password, salt, ITERATION_COUNT))
            {
                var computedHash = passwordDerivationFunction.GetBytes(HASH_SIZE_IN_BYTES);

                var hashDifference = NO_DIFFERENCE;
            
                for (int byteIndex = SOURCE_START_INDEX; byteIndex < HASH_SIZE_IN_BYTES; byteIndex++)
                {
                    hashDifference |= computedHash[byteIndex] ^ storedHash[byteIndex];
                }

                return hashDifference == NO_DIFFERENCE;
            }
        }
    }
}