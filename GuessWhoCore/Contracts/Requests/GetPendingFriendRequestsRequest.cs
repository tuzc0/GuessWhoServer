using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class GetPendingFriendRequestsRequest
    {
        [DataMember(IsRequired = true)]
        public string AccountId { get; set; }
    }
}