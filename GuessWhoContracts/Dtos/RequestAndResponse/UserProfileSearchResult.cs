using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class UserProfileSearchResult
    {
        [DataMember(IsRequired = true)] public long UserId { get; set; }
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
        [DataMember(IsRequired = true)] public string AvatarUrl { get; set; }
    }
}
