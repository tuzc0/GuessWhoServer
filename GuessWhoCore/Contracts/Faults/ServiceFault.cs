using System;
using System.Runtime.Serialization;

namespace GuessWhoCore.Contracts.Faults
{
    [DataContract]
    public sealed class ServiceFault
    {
        [DataMember(Order = 1)]
        public string Code { get; set; } = string.Empty;

        [DataMember(Order = 2)]
        public string MessageKey { get; set; } = string.Empty;

        [DataMember(Order = 3)]
        public string CorrelationId { get; set; } = string.Empty;

        [DataMember(Order = 4)]
        public string ExceptionType { get; set; } = string.Empty;

        [DataMember(Order = 5)]
        public string FallbackMessage { get; set; } = string.Empty;

        [DataMember(Order = 6)]
        public string[] Details { get; set; } = Array.Empty<string>();
    }
}
