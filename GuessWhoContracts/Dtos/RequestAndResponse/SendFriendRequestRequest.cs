using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class SendFriendRequestRequest
    {
        [DataMember(IsRequired = true)] public long FromAccountId { get; set; }
        [DataMember(IsRequired = true)] public long ToUserId { get; set; }
    }
}
