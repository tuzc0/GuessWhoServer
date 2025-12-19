using System.Runtime.Serialization;

namespace GuessWhoContracts.Dtos.RequestAndResponse
{
    [DataContract]
    public class ListOpenMatchesRequest
    {
        [DataMember(IsRequired = true)] public long UserId { get; set; }
    }
}
