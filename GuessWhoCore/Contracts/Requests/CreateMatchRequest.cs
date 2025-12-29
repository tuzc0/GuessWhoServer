using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class CreateMatchRequest
    {
        [DataMember(IsRequired = true)] public long ProfileId { get; set; }
    }
}
