using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class ResendVerificationRequest
    {
        [DataMember(IsRequired = true)] public long AccountId { get; set; }
    }
}
