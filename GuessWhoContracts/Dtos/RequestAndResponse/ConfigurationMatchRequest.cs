using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class ConfigurationMatchRequest
    {
        [DataMember(IsRequired = true)] public int MatchId { get; set; }
        [DataMember] public string Mode { get; set; }
        [DataMember] public byte Visibility { get; set; }

    }
}
