using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class ResendVerificationRequest
    {
        [DataMember(IsRequired = true)] public long AccountId { get; set; }
    }
}
