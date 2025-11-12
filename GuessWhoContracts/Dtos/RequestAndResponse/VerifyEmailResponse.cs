using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class VerifyEmailResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
    }
}
