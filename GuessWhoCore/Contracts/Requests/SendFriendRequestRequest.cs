using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class SendFriendRequestRequest
    {
        [DataMember(IsRequired = true)] public long FromAccountId { get; set; }
        [DataMember(IsRequired = true)] public long ToUserId { get; set; }
    }
}
