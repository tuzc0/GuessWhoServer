using GuessWhoContracts.Dtos.RequestAndResponse;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface ILoginService
    {
        [OperationContract]
        LoginResponse LoginUser(LoginRequest request);
    }
}
