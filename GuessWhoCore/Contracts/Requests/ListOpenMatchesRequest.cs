using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Requests
{
    [DataContract]
    public class ListOpenMatchesRequest
    {
        [DataMember(IsRequired = true)] public long UserId { get; set; }
    }
}
