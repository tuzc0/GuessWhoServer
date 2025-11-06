using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class RegisterResponse
    {
        [DataMember(IsRequired = true)] public long AccountId { get; set; }
        [DataMember(IsRequired = true)] public long UserId { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string DisplayName { get; set; }
        [DataMember] public bool EmailVerificationRequired { get; set; } = true;
    }
}
