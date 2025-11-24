using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface ILoginService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        LoginResponse LoginUser(LoginRequest request);
    }
}
