using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Request;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
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
        BasicResponse AcceptFriendRequest(FriendRequestOperationRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse RejectFriendRequest(FriendRequestOperationRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse CancelFriendRequest(FriendRequestOperationRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        GetFriendsResponse GetFriends(GetFriendsRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        GetPendingRequestsResponse GetPendingRequests(GetPendingFriendRequestsRequest request);

    }
}
