using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class UpdatePasswordRequest
    {
        [DataMember] public string Email { get; set; }

        [DataMember] public string VerificationCode { get; set; }

        [DataMember] public string NewPassword { get; set; }
    }
}