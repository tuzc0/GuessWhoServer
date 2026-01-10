using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class SendMatchInvitationRequest
    {
        [DataMember]
        public long MatchId { get; set; }
        [DataMember]
        public long InviterUserId { get; set; }
        [DataMember]
        public string TargetEmail { get; set; }
        [DataMember]
        public long TargetUserId { get; set; }
    }
}