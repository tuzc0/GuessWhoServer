using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class SearchProfilesResponse
    {
        [DataMember(IsRequired = true)] public List<UserProfileSearchResult> Profiles { get; set; }
    }
}
