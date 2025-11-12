using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class BasicResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
    }
}
