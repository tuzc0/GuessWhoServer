using GuessWhoCore.Contracts.Faults;
using log4net;
using System;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Errors;

namespace WcfServiceLibraryGuessWho.Coordinators.Base
{
    public abstract class ServiceBase
    {
        private const string ERROR_ACTION_NULL_CONTEXT = "ServiceBase.ExecuteService: action is null.";
        private const string ERROR_UNEXPECTED_CONTEXT_PREFIX = "Unexpected error in WCF service operation: ";

        protected abstract ILog Logger { get; }

        protected T ExecuteService<T>(string operationContext, Func<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            string safeContext = operationContext ?? string.Empty;

            try
            {
                return action();
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ERROR_UNEXPECTED_CONTEXT_PREFIX + safeContext, ex);

                throw TranslateTechnicalFault(ex);
            }
        }

        protected void ExecuteService(string operationContext, Action action)
        {
            if (action == null)
            {
                Logger.Error(ERROR_ACTION_NULL_CONTEXT);

                throw new ArgumentNullException(nameof(action));
            }

            ExecuteService(operationContext,() =>
            {
                action();
                return true;
            });
        }

        protected virtual FaultException<ServiceFault> TranslateTechnicalFault(Exception ex)
        {
            return FaultTranslator.ToTechnicalFault(ex, Logger);
        }
    }
}
