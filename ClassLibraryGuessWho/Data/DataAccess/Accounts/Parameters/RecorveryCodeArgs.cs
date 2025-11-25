using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters
{
    public class RecoveryCodeArgs
    {
        public long AccountId { get; set; }
        public byte[] CodeHash { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}