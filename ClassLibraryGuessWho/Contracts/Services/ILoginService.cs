using System.ServiceModel;
using ClassLibraryGuessWho.Contracts.Dtos;

namespace ClassLibraryGuessWho.Contracts.Services
{
    [ServiceContract]
    public interface ILoginService
    {
        [OperationContract]
        LoginResponse LoginUser(LoginRequest request);
    }
}
