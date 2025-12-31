using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class GetPublicLobbyMatchesResponse
    {
        [DataMember] public IReadOnlyList<PublicLobbyMatchDto> Matches { get; set; }
    }
}
