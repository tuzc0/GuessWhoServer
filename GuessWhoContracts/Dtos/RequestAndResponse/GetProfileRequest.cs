using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class GetProfileRequest
    {
        [DataMember(IsRequired = true)] public long IdAccount { get; set; }
    }
}
