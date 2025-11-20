using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class GetFriendsResponse
    {
        [DataMember]
        public List<UserProfileSearchResult> Friends { get; set; }
    }
}