using GuessWhoServerDomain.Domain.Interfaces.Security;
using System;

namespace GuessWhoServices.Security
{
    internal sealed class PasswordHash : IPasswordHasher
    {
        private const string EMPTY = "";

        public byte[] HashPassword(string password)
        {
            return Pbkdf2Sha256PasswordHashing.HashPassword(password);
        }

        public bool VerifyPassword(string password, byte[] storedPasswordHashBytes)
        {
            string safePassword = password ?? EMPTY;
            byte[] safeStored = storedPasswordHashBytes ?? Array.Empty<byte>();

            return Pbkdf2Sha256PasswordHashing.Verify(safePassword, safeStored);
        }
    }
}
