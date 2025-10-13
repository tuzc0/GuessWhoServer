using System.ServiceModel;
using GuessWho.Contracts.Dtos;

namespace GuessWho.Contracts.Services
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        RegisterResponse RegisterUser(RegisterRequest request);

    }
}