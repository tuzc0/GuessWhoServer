using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class StartMatchRequest
    {
        [DataMember(IsRequired = true)] public long MatchId { get; set; }
    }
}
