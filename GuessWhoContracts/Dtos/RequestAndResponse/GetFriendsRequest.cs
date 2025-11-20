using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class GetFriendsRequest
    {
        [DataMember(IsRequired = true)]
        public string AccountId { get; set; }
    }
}