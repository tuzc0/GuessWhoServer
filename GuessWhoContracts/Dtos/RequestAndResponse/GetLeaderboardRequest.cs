using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class GetLeaderboardRequest
    {
        [DataMember]
        public int TopN { get; set; }

        [DataMember]
        public int RequestingUserId { get; set; }
    }
}