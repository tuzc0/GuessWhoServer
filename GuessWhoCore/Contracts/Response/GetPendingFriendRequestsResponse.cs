using GuessWhoCore.Contracts.Requests;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
        [DataContract]
        public class GetPendingRequestsResponse
        {
            [DataMember]
            public List<FriendRequest> Requests { get; set; }
        }
    
}
