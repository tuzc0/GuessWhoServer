using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class AcceptFriendRequestRequest
    {
        [DataMember(IsRequired = true)] public string AccountId { get; set; }
        [DataMember(IsRequired = true)] public string FriendRequestId { get; set; }
    }
}
