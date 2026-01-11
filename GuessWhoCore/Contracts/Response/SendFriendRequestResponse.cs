using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class SendFriendRequestResponse
    {
        [DataMember(IsRequired = true)]
        public bool Success { get; set; }

        [DataMember]
        public long FriendRequestId { get; set; }

        [DataMember]
        public bool AutoAccepted { get; set; }
    }
}