using GuessWhoCore.Contracts.Faults;
using GuessWhoServices.Services.ErrorHandling;
using System.ServiceModel;
using GuessWhoServices.Communication.Email.Helpers;

namespace GuessWhoServices.Communication.Email
{
    internal static class EmailFaultTranslator
    {
        internal static FaultException<ServiceFault> ToInfrastructureEmailFault(EmailSendResult result)
        {
            if (result == null)
            {
                return FaultsFactory.Create(
                    InfrastructureEmailFaultKeys.CODE_EMAIL_UNEXPECTED_ERROR,
                    InfrastructureEmailFaultKeys.MSG_EMAIL_UNEXPECTED_ERROR,
                    InfrastructureEmailFaultKeys.FALLBACK_EMAIL_UNEXPECTED_ERROR);
            }

            string errorCode = (result.ErrorCode ?? string.Empty).Trim();

            switch (errorCode)
            {
                case EmailInternalCodes.RECIPIENT_INVALID:
                    return FaultsFactory.Create(
                        InfrastructureEmailFaultKeys.CODE_EMAIL_RECIPIENT_INVALID,
                        InfrastructureEmailFaultKeys.MSG_EMAIL_RECIPIENT_INVALID,
                        InfrastructureEmailFaultKeys.FALLBACK_EMAIL_RECIPIENT_INVALID);

                case EmailInternalCodes.AUTH_ERROR:
                    return FaultsFactory.Create(
                        InfrastructureEmailFaultKeys.CODE_EMAIL_AUTHENTICATION_FAILED,
                        InfrastructureEmailFaultKeys.MSG_EMAIL_AUTHENTICATION_FAILED,
                        InfrastructureEmailFaultKeys.FALLBACK_EMAIL_AUTHENTICATION_FAILED);

                case EmailInternalCodes.CONFIG_MISSING:
                    return FaultsFactory.Create(
                        InfrastructureEmailFaultKeys.CODE_EMAIL_CONFIGURATION_MISSING,
                        InfrastructureEmailFaultKeys.MSG_EMAIL_CONFIGURATION_MISSING,
                        InfrastructureEmailFaultKeys.FALLBACK_EMAIL_CONFIGURATION_MISSING);

                case EmailInternalCodes.TIMEOUT:
                    return FaultsFactory.Create(
                        InfrastructureEmailFaultKeys.CODE_EMAIL_TIMEOUT,
                        InfrastructureEmailFaultKeys.MSG_EMAIL_TIMEOUT,
                        InfrastructureEmailFaultKeys.FALLBACK_EMAIL_TIMEOUT);


                case EmailInternalCodes.CONFIG_ERROR:
                    return FaultsFactory.Create(
                        InfrastructureEmailFaultKeys.CODE_EMAIL_CONFIGURATION_ERROR,
                        InfrastructureEmailFaultKeys.MSG_EMAIL_CONFIGURATION_ERROR,
                        InfrastructureEmailFaultKeys.FALLBACK_EMAIL_CONFIGURATION_ERROR);

                case EmailInternalCodes.UNAVAILABLE:
                    return FaultsFactory.Create(
                        InfrastructureEmailFaultKeys.CODE_EMAIL_UNAVAILABLE,
                        InfrastructureEmailFaultKeys.MSG_EMAIL_UNAVAILABLE,
                        InfrastructureEmailFaultKeys.FALLBACK_EMAIL_UNAVAILABLE);

                case EmailInternalCodes.UNKNOWN:
                default:
                    return MapUnknownByStatus(result);
            }
        }

        private static FaultException<ServiceFault> MapUnknownByStatus(EmailSendResult result)
        {
            if (result.Status == EmailSendStatus.TemporaryFailure)
            {
                return FaultsFactory.Create(
                    InfrastructureEmailFaultKeys.CODE_EMAIL_UNAVAILABLE,
                    InfrastructureEmailFaultKeys.MSG_EMAIL_UNAVAILABLE,
                    InfrastructureEmailFaultKeys.FALLBACK_EMAIL_UNAVAILABLE);
            }

            if (result.Status == EmailSendStatus.PermanentFailure)
            {
                return FaultsFactory.Create(
                    InfrastructureEmailFaultKeys.CODE_EMAIL_SEND_FAILED,
                    InfrastructureEmailFaultKeys.MSG_EMAIL_SEND_FAILED,
                    InfrastructureEmailFaultKeys.FALLBACK_EMAIL_SEND_FAILED);
            }

            return FaultsFactory.Create(
                InfrastructureEmailFaultKeys.CODE_EMAIL_UNEXPECTED_ERROR,
                InfrastructureEmailFaultKeys.MSG_EMAIL_UNEXPECTED_ERROR,
                InfrastructureEmailFaultKeys.FALLBACK_EMAIL_UNEXPECTED_ERROR);
        }
    }
}
