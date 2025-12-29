using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response   
{
    [DataContract]
    public class GetProfileResponse
    {
        [DataMember(IsRequired = true)] public string Username { get; set; }
        [DataMember(IsRequired = true)] public string Email { get; set; }
        [DataMember(IsRequired = true)] public DateTime CreatedAtUtc { get; set; }
        [DataMember(IsRequired = true)] public string AvatarId { get; set; }
    }
}
