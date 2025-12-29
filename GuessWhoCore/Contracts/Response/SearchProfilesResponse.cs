using GuessWhoCore.Contracts.Requests;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class SearchProfilesResponse
    {
        [DataMember(IsRequired = true)] public List<UserProfileSearchResult> Profiles { get; set; }
    }
}
