using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
        [DataContract]
        public class GetPendingRequestsResponse
        {
            [DataMember]
            public List<FriendRequest> Requests { get; set; }
        }
    
}
