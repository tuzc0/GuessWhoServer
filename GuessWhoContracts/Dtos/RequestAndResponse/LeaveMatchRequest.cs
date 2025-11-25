using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class LeaveMatchRequest
    {
        [DataMember(IsRequired = true)] public long MatchId { get; set; }
        [DataMember(IsRequired = true)] public long UserId { get; set; }
    }
}
