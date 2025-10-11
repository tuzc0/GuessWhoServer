using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class LoginResponse
    {
        [DataMember(IsRequired = true)]
        public string User { get; set; }

        [DataMember(IsRequired = true)]
        public string Password { get; set; }
    }
}
