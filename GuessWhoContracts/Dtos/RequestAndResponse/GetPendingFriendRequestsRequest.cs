using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class GetPendingFriendRequestsRequest
    {
        [DataMember(IsRequired = true)]
        public string AccountId { get; set; }
    }
}