using GuessWhoCore.Contracts.Faults;
using System;
using System.ServiceModel;

namespace GuessWhoServices.Services.ErrorHandling
{
    public static class FaultsFactory
    {
        private const string GUID_FORMAT_NO_HYPHENS = "N";
        private const string EXCEPTION_TYPE_BUSINESS = "Business";
        private const string EMPTY = "";

        public static FaultException<ServiceFault> Create(string code, string messageKey, string fallbackMessage)
        {
            return Create(code, messageKey, fallbackMessage, null, null, null);
        }

        public static FaultException<ServiceFault> Create(string code)
        {
            return Create(code, null, null, null, null, null);
        }

        public static FaultException<ServiceFault> Create(string code, Exception ex)
        {
            return Create(code, null, null, ex, null, null);
        }

        public static FaultException<ServiceFault> Create(string code, string messageKey, string fallbackMessage,
            Exception ex)
        {
            return Create(code, messageKey, fallbackMessage, ex, null, null);
        }

        public static FaultException<ServiceFault> Create(string code, string messageKey, string fallbackMessage,
            Exception ex, string correlationId, string[] details)
        {
            string effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? Guid.NewGuid().ToString(GUID_FORMAT_NO_HYPHENS)
                : correlationId;

            string safeCode = code ?? EMPTY;
            string safeMessageKey = messageKey ?? EMPTY;
            string safeFallback = fallbackMessage ?? EMPTY;

            string exceptionTypeName = ex != null
                ? ex.GetType().Name
                : EXCEPTION_TYPE_BUSINESS;

            var fault = new ServiceFault
            {
                Code = safeCode,
                MessageKey = safeMessageKey,
                CorrelationId = effectiveCorrelationId,
                ExceptionType = exceptionTypeName,
                FallbackMessage = safeFallback,
                Details = details ?? Array.Empty<string>()
            };

            string reason = !string.IsNullOrWhiteSpace(fault.FallbackMessage)
                ? fault.FallbackMessage
                : fault.MessageKey;

            return new FaultException<ServiceFault>(fault, new FaultReason(reason));
        }
    }
}
