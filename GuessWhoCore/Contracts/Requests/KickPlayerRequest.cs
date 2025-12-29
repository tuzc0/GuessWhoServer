using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class KickPlayerRequest
    {
        [DataMember(IsRequired = true)] public long MatchId;
        [DataMember(IsRequired = true)] public long RequestingUserId;
        [DataMember(IsRequired = true)] public long TargetUserId;
    }
}
