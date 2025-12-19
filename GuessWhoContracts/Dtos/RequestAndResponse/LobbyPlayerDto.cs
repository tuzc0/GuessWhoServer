using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.Dto
{
    [DataContract]
    public sealed class LobbyPlayerDto
    {
        [DataMember] public long MatchId { get; set; }
        [DataMember] public long UserId { get; set; }
        [DataMember] public string DisplayName { get; set; }
        [DataMember] public string AvatarId {  get; set; }
        [DataMember] public byte SlotNumber { get; set; }
        [DataMember] public bool IsReady { get; set; }
        [DataMember] public bool IsHost { get; set; }
    }
}
