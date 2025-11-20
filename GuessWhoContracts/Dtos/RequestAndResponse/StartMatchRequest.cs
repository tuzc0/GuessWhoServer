using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class StartMatchRequest
    {
        [DataMember(IsRequired = true)] public int MatchId { get; set; }
    }
}
