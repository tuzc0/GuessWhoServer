using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    public interface IChatServiceCallback
    {
        [OperationContract(IsOneWay = true)] 
        void ReceiveMessage(string user, string message);
    }
}
