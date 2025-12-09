using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.Dto
{
    [DataContract]
    public class LeaderboardPlayerDto
    {
        [DataMember]
        public int Rank { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string AvatarId { get; set; }

        [DataMember]
        public int Wins { get; set; }
    }
}