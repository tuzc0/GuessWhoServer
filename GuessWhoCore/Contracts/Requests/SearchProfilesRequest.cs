using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class SearchProfileRequest
    {
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
    }
}
