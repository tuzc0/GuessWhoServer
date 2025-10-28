using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class JoinMatchRequest
    {
        [DataMember(IsRequired = true)] public string MatchCode { get; set; }
        [DataMember(IsRequired = true)] public long UserId { get; set; }
    }
}
