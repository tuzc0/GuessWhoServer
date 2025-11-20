using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract(CallbackContract = typeof(IChatServiceCallback))]
    public interface IChatService
    {
        [OperationContract(IsOneWay = true)] 
        void Join(string user);

        [OperationContract(IsOneWay = true)] 
        void SendMessage(string user, string message);
        
        [OperationContract(IsOneWay = true)] 
        void Leave(string user);
    }
}
