using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Response
{
    [DataContract]
    public class BasicResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
        [DataMember] public string Code { get; set; }
        [DataMember(Name = "MessageKey")] public string MeesageKey { get; set; }
    }
}
