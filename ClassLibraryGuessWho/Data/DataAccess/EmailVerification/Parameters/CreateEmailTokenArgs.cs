using System;

namespace ClassLibraryGuessWho.Data.DataAccess.EmailVerification.Parameters
{
    public sealed class CreateEmailTokenArgs
    {
        public long AccountId { get; set; }
        public byte[] CodeHash { get; set; }
        public DateTime NowUtc { get; set; }
        public TimeSpan LifeSpan { get; set; }
    }
}
