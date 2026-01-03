using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class ClaimTimeoutResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember] public string MeesageKey { get; set; }

        [DataMember] public long MatchId { get; set; }
        [DataMember] public long WinnerUserId { get; set; }
        [DataMember] public long TimedOutUserId { get; set; }
        [DataMember] public int TotalSeconds { get; set; }
        [DataMember] public int LimitSeconds { get; set; }
    }
}
