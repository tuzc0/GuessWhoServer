using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class UserProfileSearchResult
    {
        [DataMember(IsRequired = true)] public long UserId { get; set; }
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
        [DataMember(IsRequired = true)] public string AvatarId { get; set; }
    }
}
