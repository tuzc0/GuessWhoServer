using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class SearchProfilesResponse
    {
        [DataMember(IsRequired = true)] public List<UserProfileSearchResult> Profiles { get; set; }
    }
}
