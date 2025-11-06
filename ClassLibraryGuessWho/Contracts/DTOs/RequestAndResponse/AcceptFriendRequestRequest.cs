using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class AcceptFriendRequestRequest
    {
        [DataMember(IsRequired = true)] public string AccountId { get; set; }
        [DataMember(IsRequired = true)] public string FriendRequestId { get; set; }
    }
}
