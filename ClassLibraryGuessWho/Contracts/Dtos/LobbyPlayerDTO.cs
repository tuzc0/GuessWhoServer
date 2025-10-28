using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public sealed class LobbyPlayerDto
    {
        [DataMember(Order = 1)] public long UserId { get; set; }
        [DataMember(Order = 2)] public string DisplayName { get; set; } = string.Empty;
        [DataMember(Order = 3)] public int SlotNumber { get; set; }
        [DataMember(Order = 4)] public bool IsReady { get; set; }
        [DataMember(Order = 5)] public bool IsHost { get; set; }
    }
}
