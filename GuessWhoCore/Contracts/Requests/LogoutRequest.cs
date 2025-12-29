using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Request
{
    [DataContract]
    public class LogoutRequest
    {
        [DataMember(IsRequired = true)] public long UserProfileId { get; set; } 
    }
}
