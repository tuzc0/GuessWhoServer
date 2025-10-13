using System;
using System.ServiceModel;

public static class Faults
{
    public static FaultException<ServiceFault> Create(string code, string message, string correlationId = null)
    {
        var fault = new ServiceFault
        {
            Code = code,
            Message = message,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N")
        };
        return new FaultException<ServiceFault>(fault, new FaultReason(message));
    }
}