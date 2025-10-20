using ClassLibraryGuessWho.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfServiceLibraryGuessWho.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Reentrant,
        UseSynchronizationContext = false)]
    public class ChatService : IChatService
    {
        // Lista de clientes conectados
        private readonly List<IChatServiceCallback> clients = new List<IChatServiceCallback>();

        public void Join(string user)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IChatServiceCallback>();
            var channel = (IClientChannel)callback;

            // Evita registrar el mismo cliente varias veces
            if (!clients.Exists(c => ((IClientChannel)c).SessionId == channel.SessionId))
                clients.Add(callback);

            Broadcast($"{user} se unió al chat", "Servidor");
        }

        public void SendMessage(string user, string message)
        {
            Broadcast(message, user);
        }

        public void Leave(string user)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IChatServiceCallback>();
            clients.Remove(callback);
            Broadcast($"{user} salió del chat", "Servidor");
        }

        private void Broadcast(string message, string user)
        {
            foreach (var client in clients.ToArray())
            {
                try
                {
                    client.ReceiveMessage(user, message);
                }
                catch
                {
                    // El cliente se desconectó, lo eliminamos
                    clients.Remove(client);
                }
            }
        }
    }
}
