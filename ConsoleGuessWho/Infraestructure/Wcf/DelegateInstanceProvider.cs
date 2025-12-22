using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ConsoleGuessWho.Infraestructure.Wcf
{
    public class DelegateInstanceProvider : IInstanceProvider
    {
        private readonly Func<object> instanceFactory;

        public DelegateInstanceProvider(Func<object> instanceFactory)
        {
            this.instanceFactory = instanceFactory ??
                throw new ArgumentNullException(nameof(instanceFactory));
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return instanceFactory();
        }

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            return instanceFactory();
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
