using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class SearchPublicMatchResponse
    {
        [DataMember(IsRequired = true)] public PublicLobbyMatchDto Match { get; set; }
    }
}
