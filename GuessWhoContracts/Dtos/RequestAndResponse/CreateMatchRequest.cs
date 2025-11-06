using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class CreateMatchRequest
    {
        [DataMember(IsRequired = true)] public long ProfileId { get; set; }
    }
}
