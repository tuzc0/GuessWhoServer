using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class CreateMatchResponse
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember] public byte StatusId { get; set; }
        [DataMember] public byte VisibilityId { get; set; }
        [DataMember] public byte ModeId { get; set; }
        [DataMember] public DateTime CreateAtUtc { get; set; }
        [DataMember] public long HostProfileId { get; set; }
    }
}
