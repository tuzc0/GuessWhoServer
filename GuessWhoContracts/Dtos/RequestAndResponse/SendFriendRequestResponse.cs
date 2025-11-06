using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class SendFriendRequestResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
        [DataMember] public string FriendRequestId { get; set; }
        [DataMember] public bool AutoAccepted { get; set; }
    }
}
