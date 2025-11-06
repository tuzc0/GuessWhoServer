using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Dtos
{
    [DataContract]
    public class BasicResponse
    {
        [DataMember(IsRequired = true)] public bool Success { get; set; }
    }
}
