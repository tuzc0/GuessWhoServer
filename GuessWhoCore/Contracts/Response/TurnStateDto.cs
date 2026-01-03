using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class TurnStateDto
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public int TurnNumber { get; set; }
        [DataMember] public byte CurrentPosition { get; set; }
        [DataMember] public long CurrentUserId { get; set; }
        [DataMember] public DateTime TurnStartedAtUtc { get; set; }
    }
}
