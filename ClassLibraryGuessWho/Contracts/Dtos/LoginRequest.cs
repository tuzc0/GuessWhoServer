using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class LoginRequest
    {
        [DataMember(IsRequired = true)]
        public string User { get; set; }

        [DataMember(IsRequired = true)]
        public string Password { get; set; }
    }
}
