using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class SearchPublicMatchResponse
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember] public byte StatusId { get; set; }
        [DataMember] public byte VisibilityId { get; set; }
        [DataMember] public byte ModeId { get; set; }
        [DataMember] public DateTime CreateAtUtc { get; set; }
    }
}
