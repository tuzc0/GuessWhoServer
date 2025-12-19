using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class LogoutRequest
    {
        [DataMember(IsRequired = true)] public long UserProfileId { get; set; } 
    }
}
