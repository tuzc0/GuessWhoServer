using GuessWhoContracts.Dtos.RequestAndResponse;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IProfileService
    {
        [OperationContract]
        ProfileResponse Profile(ProfileRequest request);
    }
}
