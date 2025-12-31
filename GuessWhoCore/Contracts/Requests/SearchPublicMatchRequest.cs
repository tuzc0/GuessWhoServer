using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class SearchPublicMatchRequest
    {
        [DataMember(IsRequired = true)] public string MatchCode { get; set; }
    }
}
