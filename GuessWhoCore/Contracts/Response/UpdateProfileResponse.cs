using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class UpdateProfileResponse
    {
        [DataMember(IsRequired = true)] public bool Updated { get; set; }
        [DataMember(IsRequired = true)] public string Email { get; set; }
        [DataMember(IsRequired = true)] public string Username { get; set; }
        [DataMember(IsRequired = true)] public DateTime UpdatedAtUtc { get; set; }

        [DataMember(IsRequired = true)] public string AvatarId { get; set; }
    }
}
