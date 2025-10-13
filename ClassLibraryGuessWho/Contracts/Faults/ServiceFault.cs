using System;
using System.Runtime.Serialization;
using System.ServiceModel;

[DataContract]
public sealed class ServiceFault
{
    [DataMember(Order = 1)] public string Code { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; }
    [DataMember(Order = 3)] public string CorrelationId { get; set; }
}
