using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class SearchProfileRequest
    {
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
    }
}



