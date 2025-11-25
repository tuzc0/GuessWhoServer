using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class LoginRequest
    {
        [DataMember(IsRequired = true)] public string User { get; set; }

        [DataMember(IsRequired = true)] public string Password { get; set; }
    }
}
