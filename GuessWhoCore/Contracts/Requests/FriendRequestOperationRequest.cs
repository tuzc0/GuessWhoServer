using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class FriendRequestOperationRequest
    {
        [DataMember(IsRequired = true)] public long AccountId { get; set; }
        [DataMember(IsRequired = true)] public long FriendRequestId { get; set; }
    }
}
