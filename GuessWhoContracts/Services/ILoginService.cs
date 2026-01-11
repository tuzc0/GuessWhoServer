using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Request;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface ILoginService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        LoginResponse LoginUser(LoginRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse LogoutUser(LogoutRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse TouchPresence(TouchPresenceRequest request);
    }
}
