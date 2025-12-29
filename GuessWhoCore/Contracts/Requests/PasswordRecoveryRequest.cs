using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class PasswordRecoveryRequest
    {
        [DataMember(IsRequired = true)] public string Email { get; set; }
    }
}