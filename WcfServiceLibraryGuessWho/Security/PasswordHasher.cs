using GuessWhoServerDomain.Domain.Interfaces.Security;
using System;

namespace GuessWhoServices.Security
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const string EMPTY = "";

        public PasswordHasher()
        {
        }

        public byte[] HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            return Pbkdf2Sha256PasswordHashing.HashPassword(password);
        }

        public bool VerifyPassword(string password, byte[] storedPasswordHashBytes)
        {
            if (storedPasswordHashBytes == null || storedPasswordHashBytes.Length == 0)
            {
                return false;
            }

            string safePassword = password ?? EMPTY;

            return Pbkdf2Sha256PasswordHashing.Verify(safePassword, storedPasswordHashBytes);
        }
    }
}
