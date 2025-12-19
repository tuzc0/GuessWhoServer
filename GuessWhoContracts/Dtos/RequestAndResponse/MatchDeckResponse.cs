using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class MatchDeckResponse
    {
        [DataMember]
        public string[] CharacterIds { get; set; }
    }
}
