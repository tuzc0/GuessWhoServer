using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class SetPlayerReadyStatusRequest
    {
        [DataMember] public int MatchId { get; set; }
        [DataMember] public long UserId { get; set; }
        [DataMember] public bool IsReady { get; set; }
    }
}
