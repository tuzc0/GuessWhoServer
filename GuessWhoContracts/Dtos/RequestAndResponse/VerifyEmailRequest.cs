using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class VerifyEmailRequest
    {
        [DataMember(IsRequired = true)] public long AccountId { get; set; }
        [DataMember(IsRequired = true)] public string Code { get; set; }
    }
}
