using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class LoginResponse
    {
        [DataMember] public long UserId { get; set; }
        [DataMember] public string DisplayName { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public bool ValidUser { get; set; }
    }
}
