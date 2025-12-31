using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class GetPublicLobbyMatchesRequest
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public string MatchCode { get; set; }
        [DataMember] public byte StatusId { get; set; }
        [DataMember] public byte VisibilityId { get; set; }
        [DataMember] public byte ModeId { get; set; }
        [DataMember] public DateTime CreateAtUtc { get; set; }
    }
}
