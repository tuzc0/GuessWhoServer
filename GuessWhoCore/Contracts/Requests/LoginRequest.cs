using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class LoginRequest
    {
        [DataMember(IsRequired = true)] public string Email { get; set; }

        [DataMember(IsRequired = true)] public string Password { get; set; }
    }
}
