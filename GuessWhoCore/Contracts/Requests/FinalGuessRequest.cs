using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class FinalGuessRequest
    {
        [DataMember(IsRequired = true)] public long MatchId { get; set; }
        [DataMember(IsRequired = true)] public long UserId { get; set; }
        [DataMember(IsRequired = true)] public string GuessedCharacterId { get; set; }
    }
}
