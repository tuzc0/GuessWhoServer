using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public sealed class SubscribeLobbyRequest
    {
        [DataMember(IsRequired = true, Order = 1)]
        public long MatchId { get; set; }

        [DataMember(IsRequired = true, Order = 2)]
        public long UserId { get; set; }
    }
}
