using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class TouchPresenceRequest
    {
        [DataMember] public long UserId { get; set; }
    }
}
