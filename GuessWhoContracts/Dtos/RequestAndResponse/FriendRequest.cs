using System;
using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class FriendRequest
    {
        [DataMember]
        public long FriendRequestId { get; set; }

        [DataMember]
        public long RequesterUserId { get; set; }

        [DataMember]
        public string RequesterDisplayName { get; set; }

        [DataMember]
        public long AddresseeUserId { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }
    }
}