using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class GetLeaderboardResponse
    {
        [DataMember]
        public List<LeaderboardPlayerDto> Players { get; set; }

        [DataMember]
        public LeaderboardPlayerDto CurrentUserStats { get; set; }
    }
}