using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class ListOpenMatchesResponse
    {
        [DataMember] public List<MatchDto> Matches { get; set; }
    }
}
