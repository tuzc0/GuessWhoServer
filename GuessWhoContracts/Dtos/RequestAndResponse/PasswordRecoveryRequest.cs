using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class PasswordRecoveryRequest
    {
        [DataMember(IsRequired = true)] public string Email { get; set; }
    }
}