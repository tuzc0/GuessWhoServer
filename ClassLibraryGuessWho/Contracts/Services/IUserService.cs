using System.ServiceModel;
using GuessWho.Contracts.Dtos;

namespace GuessWho.Contracts.Services
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        RegisterResponse RegisterUser(RegisterRequest request);

    }
}
