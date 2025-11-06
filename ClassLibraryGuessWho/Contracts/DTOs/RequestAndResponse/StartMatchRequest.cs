using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class StartMatchRequest
    {
        [DataMember(IsRequired = true)] public int MatchId { get; set; }
    }
}
