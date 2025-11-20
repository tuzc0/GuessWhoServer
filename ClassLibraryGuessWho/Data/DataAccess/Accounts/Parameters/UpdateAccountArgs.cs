using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts.Parameters
{
    public sealed class UpdateAccountArgs
    {
        public long UserId { get; set; }                 
        public string NewDisplayName { get; set; }
        public byte[] NewPassword { get; set; }     
        public string NewAvatarId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }          
    }
}
