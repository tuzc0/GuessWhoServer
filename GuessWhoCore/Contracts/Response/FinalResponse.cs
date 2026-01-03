using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public sealed class FinalGuessResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember] public string MeesageKey { get; set; }

        [DataMember] public bool IsCorrect { get; set; }
        [DataMember] public long WinnerUserId { get; set; }
    }
}
