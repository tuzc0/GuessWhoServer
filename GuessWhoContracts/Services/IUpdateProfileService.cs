using GuessWhoContracts.Dtos.RequestAndResponse;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IUpdateProfileService
    {
        [OperationContract]
        GetProfileResponse GetProfile(GetProfileRequest request);
        
        [OperationContract]
        UpdateProfileResponse UpdateUserProfile(UpdateProfileRequest request);
    }
}
