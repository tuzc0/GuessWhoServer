using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class CreateMatchResponse
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember] public byte StatusId { get; set; }
        [DataMember] public string Mode { get; set; }
        [DataMember] public byte Visibility { get; set; }
        [DataMember] public DateTime CreateAtUtc { get; set; }
        public List<LobbyPlayerDto> Players { get; set; }
    }
}
