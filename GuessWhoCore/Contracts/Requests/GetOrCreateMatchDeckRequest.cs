using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class GetOrCreateMatchDeckRequest
    {
        [DataMember(IsRequired = true, Order = 1)] public long MatchId { get; set; }
        [DataMember(IsRequired = true, Order = 2)] public byte ModeId { get; set; }
    }
}
