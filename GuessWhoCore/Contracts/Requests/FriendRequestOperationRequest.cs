using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class FriendRequestOperationRequest
    {
        [DataMember(IsRequired = true)] public string AccountId { get; set; }
        [DataMember(IsRequired = true)] public string FriendRequestId { get; set; }
    }
}
