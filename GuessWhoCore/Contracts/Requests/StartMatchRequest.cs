using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class StartMatchRequest
    {
        [DataMember(IsRequired = true)] public long MatchId { get; set; }
    }
}
