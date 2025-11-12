using System;
using System.ServiceModel;

namespace GuessWhoContracts.Faults
{
    public static class Faults
    {
        private const string GUID_FORMAT_NO_HYPHENS = "N";

        public static FaultException<ServiceFault> Create(string code, string message, Exception ex = null, string correlationId = null)
        {
            string effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? Guid.NewGuid().ToString(GUID_FORMAT_NO_HYPHENS)
                : correlationId;

            string exceptionTypeName = ex?.GetType().Name;

            var fault = new ServiceFault
            {
                Code = code,
                Message = message,
                CorrelationId = effectiveCorrelationId,
                ExceptionType = exceptionTypeName
            };

            return new FaultException<ServiceFault>(fault, new FaultReason(message));
        }

        public static FaultException<ServiceFault> Create(string code, string message)
        {
            return Create(code, message, null, null);
        }
    }
}
