using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class VerifyEmailResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
    }
}
