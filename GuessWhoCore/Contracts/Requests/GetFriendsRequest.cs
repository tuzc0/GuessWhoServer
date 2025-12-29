using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class GetFriendsRequest
    {
        [DataMember(IsRequired = true)]
        public string AccountId { get; set; }
    }
}