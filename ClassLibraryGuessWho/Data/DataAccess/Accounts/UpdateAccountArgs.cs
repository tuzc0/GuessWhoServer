using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Accounts
{
    public sealed class UpdateAccountArgs
    {
        public long UserId { get; set; }                 
        public string NewDisplayName { get; set; }          
        public byte[] NewPassword { get; set; }             
        public DateTime UpdatedAtUtc { get; set; }          
    }
}
