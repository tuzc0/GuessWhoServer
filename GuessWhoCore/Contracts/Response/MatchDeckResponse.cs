using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class MatchDeckResponse
    {
        [DataMember] public bool Success { get; set; }
        [DataMember] public string Code { get; set;}
        [DataMember]public List<string> CharacterIds { get; set; }
    }
}
