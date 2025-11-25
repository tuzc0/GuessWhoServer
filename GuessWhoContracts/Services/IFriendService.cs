using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IFriendService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        SearchProfilesResponse SearchProfiles(SearchProfileRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        SendFriendRequestResponse SendFriendRequest(SendFriendRequestRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse AcceptFriendRequest(AcceptFriendRequestRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse RejectFriendRequest(AcceptFriendRequestRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse CancelFriendRequest(AcceptFriendRequestRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        GetFriendsResponse GetFriends(GetFriendsRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        GetPendingRequestsResponse GetPendingRequests(GetPendingFriendRequestsRequest request);

    }
}
