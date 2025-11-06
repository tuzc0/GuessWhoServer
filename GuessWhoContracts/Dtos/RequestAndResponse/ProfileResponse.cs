using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    public class ProfileResponse
    {
        [DataMember(IsRequired = true)] public string Username { get; set; }
        [DataMember(IsRequired = true)] public string Email { get; set; }
        [DataMember(IsRequired = true)] public string Password { get; set; }
    }
}
