using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class CreateMatchRequest
    {
        [DataMember(IsRequired = true)] public long ProfileId { get; set; }
    }
}
