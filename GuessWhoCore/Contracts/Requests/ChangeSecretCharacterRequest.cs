using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class ChangeSecretCharacterRequest
    {
        [DataMember(IsRequired = true)] public long MatchId { get; set; }
        [DataMember(IsRequired = true)] public long ProfileId { get; set; }
        [DataMember(IsRequired = true)] public string CharacterId { get; set; }
    }
}
