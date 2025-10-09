using System;
using System.Security.Cryptography;

namespace GuessWho.Services.WCF.Security
{
    internal static class PasswordHasher
    {
        public static byte[] HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var salt = new byte[16];

            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                randomGenerator.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                var hash = pbkdf2.GetBytes(32);

                var result = new byte[1 + salt.Length + hash.Length];
                result[0] = 1; 

                Buffer.BlockCopy(salt, 0, result, 1, salt.Length);
                Buffer.BlockCopy(hash, 0, result, 1 + salt.Length, hash.Length);

                return result;
            }
        }

        public static bool Verify(string password, byte[] stored)
        {
            if (stored == null || stored.Length != 1 + 16 + 32)
            {
                return false;
            }

            var version = stored[0];
            if (version != 1)
            {
                return false;
            }

            var salt = new byte[16];
            var hash = new byte[32];

            Buffer.BlockCopy(stored, 1, salt, 0, 16);
            Buffer.BlockCopy(stored, 17, hash, 0, 32);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                var computed = pbkdf2.GetBytes(32);

                var difference = 0;
                for (int i = 0; i < 32; i++)
                {
                    difference |= computed[i] ^ hash[i];
                }

                return difference == 0;
            }
        }
    }
}
