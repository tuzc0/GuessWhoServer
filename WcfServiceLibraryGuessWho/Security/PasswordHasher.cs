using System;
using System.Security.Cryptography;

namespace GuessWho.Services.WCF.Security
{
    internal static class PasswordHasher
    {
        private const int SaltSizeInBytes = 32;
        private const int HashSizeInBytes = 32;
        private const int IterationCountDefault = 600_000;

        private const int MinimumIterationCount = 1;
        private const int MinimumSaltLengthInBytes = 1;
        private const int MinimumHashLengthInBytes = 1;

        private const byte CurrentVersion = 1;
        private const byte AlgorithmSha256 = 1;

        private const int VersionSizeInBytes = 1;
        private const int AlgorithmSizeInBytes = 1;
        private const int IterationsSizeInBytes = 4;
        private const int SaltLengthSizeInBytes = 1;

        private const int VersionIndex = 0;
        private const int AlgorithmIndex = VersionIndex + VersionSizeInBytes;
        private const int IterationsStartIndex = AlgorithmIndex + AlgorithmSizeInBytes;
        private const int SaltLengthIndex = IterationsStartIndex + IterationsSizeInBytes;

        private const int ByteMask = 0xFF;

        private const int Int32Byte0ShiftBits = 24;
        private const int Int32Byte1ShiftBits = 16;
        private const int Int32Byte2ShiftBits = 8;
        private const int Int32Byte3ShiftBits = 0;

        private const int NoDifferenceValue = 0;
        private const int ArrayCopySourceStartIndex = 0;

        private static readonly int HeaderSizeInBytes = VersionSizeInBytes + AlgorithmSizeInBytes 
            + IterationsSizeInBytes + SaltLengthSizeInBytes;

        private static readonly int MinimumStoredHashLengthInBytes = HeaderSizeInBytes
            + MinimumSaltLengthInBytes + MinimumHashLengthInBytes;

        private static readonly RandomNumberGenerator SecureRandomGenerator = RandomNumberGenerator.Create();

        private static byte[] GenerateSalt(int saltSizeInBytes)
        {
            var saltBytes = new byte[saltSizeInBytes];
            SecureRandomGenerator.GetBytes(saltBytes);

            return saltBytes;
        }

        public static byte[] HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var saltBytes = GenerateSalt(SaltSizeInBytes);

            byte[] hashBytes;

            using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, saltBytes,
                       IterationCountDefault, HashAlgorithmName.SHA256))
            {
                hashBytes = keyDerivationFunction.GetBytes(HashSizeInBytes);
            }

            var storedPasswordHash = new byte[HeaderSizeInBytes + saltBytes.Length + hashBytes.Length];

            storedPasswordHash[VersionIndex] = CurrentVersion;
            storedPasswordHash[AlgorithmIndex] = AlgorithmSha256;

            WriteInt32BigEndian(storedPasswordHash, IterationsStartIndex, IterationCountDefault);
            storedPasswordHash[SaltLengthIndex] = (byte)saltBytes.Length;

            var saltStartIndex = HeaderSizeInBytes;

            Buffer.BlockCopy(saltBytes, ArrayCopySourceStartIndex, storedPasswordHash,
                saltStartIndex, saltBytes.Length);

            var hashStartIndex = saltStartIndex + saltBytes.Length;

            Buffer.BlockCopy(hashBytes, ArrayCopySourceStartIndex, storedPasswordHash,
                hashStartIndex, hashBytes.Length);

            return storedPasswordHash;
        }

        public static bool Verify(string password, byte[] storedPasswordHashBytes)
        {
            if (string.IsNullOrEmpty(password) || storedPasswordHashBytes == null)
            {
                return false;
            }

            var passwordHashRecord = PasswordHashRecord.FromStoredPasswordHash(storedPasswordHashBytes);

            if (!passwordHashRecord.IsValid)
            {
                return false;
            }

            var computedHashBytes = ComputeHash(password, passwordHashRecord.SaltBytes,
                passwordHashRecord.IterationCount, passwordHashRecord.HashBytes.Length);

            return AreHashesEqual(passwordHashRecord.HashBytes, computedHashBytes);
        }

        private static byte[] ComputeHash(string password, byte[] saltBytes, int iterationCount,
            int hashLengthInBytes)
        {
            using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, saltBytes,
                       iterationCount, HashAlgorithmName.SHA256))
            {
                return keyDerivationFunction.GetBytes(hashLengthInBytes);
            }
        }

        private static bool AreHashesEqual(byte[] storedHashBytes, byte[] computedHashBytes)
        {
            if (storedHashBytes == null || computedHashBytes == null ||
                storedHashBytes.Length != computedHashBytes.Length)
            {
                return false;
            }

            var differenceAccumulator = NoDifferenceValue;

            for (var index = ArrayCopySourceStartIndex; index < storedHashBytes.Length; index++)
            {
                differenceAccumulator |= computedHashBytes[index] ^ storedHashBytes[index];
            }

            return differenceAccumulator == NoDifferenceValue;
        }

        private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset + 0] = (byte)((value >> Int32Byte0ShiftBits) & ByteMask);
            buffer[offset + 1] = (byte)((value >> Int32Byte1ShiftBits) & ByteMask);
            buffer[offset + 2] = (byte)((value >> Int32Byte2ShiftBits) & ByteMask);
            buffer[offset + 3] = (byte)((value >> Int32Byte3ShiftBits) & ByteMask);
        }

        private sealed class PasswordHashRecord
        {
            private PasswordHashRecord(bool isValid, int iterationCount, byte[] saltBytes, byte[] hashBytes)
            {
                IsValid = isValid;
                IterationCount = iterationCount;
                SaltBytes = saltBytes;
                HashBytes = hashBytes;
            }

            public bool IsValid { get; }

            public int IterationCount { get; }

            public byte[] SaltBytes { get; }

            public byte[] HashBytes { get; }

            public static PasswordHashRecord FromStoredPasswordHash(byte[] storedPasswordHashBytes)
            {
                if (!IsStoredPasswordHashLengthValid(storedPasswordHashBytes))
                {
                    return CreateInvalid();
                }

                if (!IsVersionSupported(storedPasswordHashBytes))
                {
                    return CreateInvalid();
                }

                if (!IsAlgorithmSupported(storedPasswordHashBytes))
                {
                    return CreateInvalid();
                }

                var iterationCount = ReadInt32BigEndian(storedPasswordHashBytes, IterationsStartIndex);

                if (!IsIterationCountValid(iterationCount))
                {
                    return CreateInvalid();
                }

                var saltLengthInBytes = storedPasswordHashBytes[SaltLengthIndex];

                if (!IsSaltLengthAndTotalLengthValid(saltLengthInBytes, storedPasswordHashBytes.Length))
                {
                    return CreateInvalid();
                }

                var saltStartIndex = HeaderSizeInBytes;

                var saltBytes = ExtractSaltBytes(storedPasswordHashBytes, saltStartIndex, saltLengthInBytes);

                var storedHashLengthInBytes =
                    CalculateStoredHashLengthInBytes(storedPasswordHashBytes.Length, saltLengthInBytes);

                if (!IsStoredHashLengthValid(storedHashLengthInBytes))
                {
                    return CreateInvalid();
                }

                var hashStartIndex = saltStartIndex + saltLengthInBytes;

                var storedHashBytes = ExtractStoredHashBytes(storedPasswordHashBytes, hashStartIndex,
                    storedHashLengthInBytes);

                return new PasswordHashRecord(isValid: true, iterationCount: iterationCount,
                    saltBytes: saltBytes, hashBytes: storedHashBytes);
            }

            private static PasswordHashRecord CreateInvalid()
            {
                return new PasswordHashRecord(isValid: false, iterationCount: 0,
                    saltBytes: null, hashBytes: null);
            }

            private static bool IsStoredPasswordHashLengthValid(byte[] storedPasswordHashBytes)
            {
                return storedPasswordHashBytes.Length >= MinimumStoredHashLengthInBytes;
            }

            private static bool IsVersionSupported(byte[] storedPasswordHashBytes)
            {
                var storedVersion = storedPasswordHashBytes[VersionIndex];
                return storedVersion == CurrentVersion;
            }

            private static bool IsAlgorithmSupported(byte[] storedPasswordHashBytes)
            {
                var storedAlgorithm = storedPasswordHashBytes[AlgorithmIndex];
                return storedAlgorithm == AlgorithmSha256;
            }

            private static bool IsIterationCountValid(int iterationCount)
            {
                return iterationCount >= MinimumIterationCount;
            }

            private static bool IsSaltLengthAndTotalLengthValid(int saltLengthInBytes, int totalLengthInBytes)
            {
                var expectedMinimumLength = HeaderSizeInBytes + saltLengthInBytes + MinimumHashLengthInBytes;

                return saltLengthInBytes >= MinimumSaltLengthInBytes
                       && totalLengthInBytes >= expectedMinimumLength;
            }

            private static bool IsStoredHashLengthValid(int storedHashLengthInBytes)
            {
                return storedHashLengthInBytes >= MinimumHashLengthInBytes;
            }

            private static byte[] ExtractSaltBytes(byte[] storedPasswordHashBytes, int saltStartIndex,
                int saltLengthInBytes)
            {
                var saltBytes = new byte[saltLengthInBytes];

                Buffer.BlockCopy(storedPasswordHashBytes, saltStartIndex, saltBytes,
                    ArrayCopySourceStartIndex, saltLengthInBytes);

                return saltBytes;
            }

            private static int CalculateStoredHashLengthInBytes(int totalLengthInBytes, int saltLengthInBytes)
            {
                return totalLengthInBytes - (HeaderSizeInBytes + saltLengthInBytes);
            }

            private static byte[] ExtractStoredHashBytes(byte[] storedPasswordHashBytes, int hashStartIndex,
                int storedHashLengthInBytes)
            {
                var storedHashBytes = new byte[storedHashLengthInBytes];

                Buffer.BlockCopy(storedPasswordHashBytes, hashStartIndex, storedHashBytes,
                    ArrayCopySourceStartIndex, storedHashLengthInBytes);

                return storedHashBytes;
            }

            private static int ReadInt32BigEndian(byte[] buffer, int offset)
            {
                var byte0 = buffer[offset + 0] << Int32Byte0ShiftBits;
                var byte1 = buffer[offset + 1] << Int32Byte1ShiftBits;
                var byte2 = buffer[offset + 2] << Int32Byte2ShiftBits;
                var byte3 = buffer[offset + 3] << Int32Byte3ShiftBits;

                return byte0 | byte1 | byte2 | byte3;
            }
        }
    }
}
