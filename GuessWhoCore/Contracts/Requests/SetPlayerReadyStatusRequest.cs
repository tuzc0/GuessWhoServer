using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class SetPlayerReadyStatusRequest
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public long UserId { get; set; }
        [DataMember] public bool IsReady { get; set; }
    }
}
