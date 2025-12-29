using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class MatchDeckResponse
    {
        [DataMember]
        public string[] CharacterIds { get; set; }
    }
}
