using GuessWhoCore.Contracts.Requests;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class GetFriendsResponse
    {
        [DataMember]
        public List<UserProfileSearchResult> Friends { get; set; }
    }
}