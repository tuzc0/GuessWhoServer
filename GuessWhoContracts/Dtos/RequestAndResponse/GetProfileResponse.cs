using System;
using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    public class GetProfileResponse
    {
        [DataMember(IsRequired = true)] public string Username { get; set; }
        [DataMember(IsRequired = true)] public string Email { get; set; }
        [DataMember(IsRequired = true)] public DateTime CreateAtUtc { get; set; }
        [DataMember(IsRequired = true)] public string AvatarURL { get; set; }
    }
}
