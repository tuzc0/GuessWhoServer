using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class RegisterRequest
    {
        [DataMember(IsRequired = true)] public string Email { get; set; }
        [DataMember(IsRequired = true)] public string Password { get; set; }
        [DataMember(IsRequired = true)] public string DisplayName { get; set; }
    }
}
