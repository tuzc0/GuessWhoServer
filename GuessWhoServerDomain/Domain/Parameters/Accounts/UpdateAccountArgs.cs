using System;

namespace GuessWhoServerDomain.Domain.Parameters.Accounts
{
    public sealed class UpdateAccountArgs
    {
        public long AccountId { get; set; }                 
        public string NewDisplayName { get; set; }
        public byte[] NewPasswordHash { get; set; }     
        public string NewAvatarId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }          
    }
}
