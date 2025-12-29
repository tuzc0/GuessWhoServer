using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class VerifyEmailRequest
    {
        [DataMember(IsRequired = true)] public long AccountId { get; set; }
        [DataMember(IsRequired = true)] public string Code { get; set; }
    }
}
