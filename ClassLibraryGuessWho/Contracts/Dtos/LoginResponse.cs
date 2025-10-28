using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class LoginResponse
    {
        [DataMember] public string User { get; set; }

        [DataMember] public string Password { get; set; }

        [DataMember(IsRequired = false)] public string ValidUser { get; set; }
    }
}
