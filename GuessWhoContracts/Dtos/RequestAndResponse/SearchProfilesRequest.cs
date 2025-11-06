using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class SearchProfileRequest
    {
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
    }
}
