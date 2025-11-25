using System;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    public sealed class UpdateProfileRequest
    {
        public long UserId { get; set; }              
        public string NewDisplayName { get; set; }      
        public string NewPasswordPlain { get; set; }      
        public string CurrentPasswordPlain { get; set; }  
        public DateTime? IfUnmodifiedSinceUtc { get; set; } 
        public string NewAvatarId { get; set; }
    }
}
