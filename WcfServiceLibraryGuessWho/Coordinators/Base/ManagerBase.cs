using GuessWhoCore.Contracts.Faults;
using log4net;
using System;
using System.ServiceModel;

namespace WcfServiceLibraryGuessWho.Coordinators.Base
{
    public abstract class ManagerBase
    {
        protected abstract ILog Logger { get; }

        protected T ExecuteService<T>(string context, Func<T> action)
        {
            return ExecuteService(context, action, customErrorHandler: null);
        }

        protected void ExecuteService(string context, Action action)
        {
            ExecuteService(context, () =>
            {
                action();
                return true;
            }, customErrorHandler: null);
        }

        protected T ExecuteService<T>(
            string context,
            Func<T> action,
            Action<Exception> customErrorHandler)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

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
                TryRunCustomHandler(customErrorHandler, ex);

                Logger.Error(context ?? string.Empty, ex);

                throw TranslateTechnicalFault(ex);
            }
        }

        protected void ExecuteService(
            string context,
            Action action,
            Action<Exception> customErrorHandler)
        {
            ExecuteService(context, () =>
            {
                action();
                return true;
            }, customErrorHandler);
        }

        private static void TryRunCustomHandler(Action<Exception> customErrorHandler, Exception ex)
        {
            if (customErrorHandler == null)
            {
                return;
            }

            try
            {
                customErrorHandler(ex);
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
        }

        protected abstract FaultException<ServiceFault> TranslateTechnicalFault(Exception ex);
    }
}
