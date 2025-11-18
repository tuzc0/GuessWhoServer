using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IUpdateProfileService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        GetProfileResponse GetProfile(GetProfileRequest request);
        
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        UpdateProfileResponse UpdateUserProfile(UpdateProfileRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse DeleteUserProfile(DeleteProfileRequest request);
    }
}
