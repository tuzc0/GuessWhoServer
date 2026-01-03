using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class PassTurnRequest
    {
        [DataMember(IsRequired = true)] public long MatchId { get; set; }
        [DataMember(IsRequired = true)] public long UserId { get; set; }

        [DataMember] public byte[] ExpectedRowVersion { get; set; }
    }
}
