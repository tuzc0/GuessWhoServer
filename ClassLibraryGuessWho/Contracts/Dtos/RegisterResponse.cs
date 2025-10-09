using System.Runtime.Serialization;

namespace GuessWho.Contracts.Dtos
{
    [DataContract]
    public class RegisterResponse
    {
        [DataMember] public long AccountId { get; set; }
        [DataMember] public long UserId { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string DisplayName { get; set; }
        [DataMember] public bool EmailVerificationRequired { get; set; } = true;
    }
}
