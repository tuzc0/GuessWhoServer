using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class PublicLobbyMatchDto
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public string MatchCode { get; set; }
        [DataMember] public byte StatusId { get; set; }
        [DataMember] public byte VisibilityId { get; set; }
        [DataMember] public byte ModeId { get; set; }
        [DataMember] public DateTime CreatedAtUtc { get; set; }
    }
}
