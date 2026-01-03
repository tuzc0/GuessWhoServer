using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class PassTurnResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember] public string MeesageKey { get; set; }

        [DataMember] public TurnStateDto TurnState { get; set; }

        [DataMember] public int SecondsConsumed { get; set; }
        [DataMember] public int LimitSeconds { get; set; }

        [DataMember] public bool IsTimeOut { get; set; }
        [DataMember] public long WinnerUserId { get; set; }
    }
}
