using System.Runtime.Serialization;

namespace ClassLibraryGuessWho.Contracts.Faults
{
    [DataContract]
    public sealed class ServiceFault
    {
        [DataMember(Order = 1)] public string Code { get; set; }
        [DataMember(Order = 2)] public string Message { get; set; }
        [DataMember(Order = 3)] public string CorrelationId { get; set; }

        [DataMember(Order = 4)] public string ExceptionType { get; set; }
    }
}