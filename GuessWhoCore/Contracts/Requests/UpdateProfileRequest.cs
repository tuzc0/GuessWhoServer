using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class UpdateProfileRequest
    {
        [DataMember(IsRequired = true)] public long UserId { get; set; }              
        [DataMember] public string NewDisplayName { get; set; }      
        [DataMember] public string NewPasswordPlain { get; set; }      
        [DataMember] public string CurrentPasswordPlain { get; set; }  
        [DataMember] public DateTime? IfUnmodifiedSinceUtc { get; set; } 
        [DataMember] public string NewAvatarId { get; set; }
    }
}
