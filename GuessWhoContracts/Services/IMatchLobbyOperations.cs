using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IMatchLobbyOperations
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        CreateMatchResponse CreateMatch(CreateMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        JoinMatchResponse JoinMatch(JoinMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse LeaveMatch(LeaveMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse SetMatchVisibility(SetMatchVisibilityRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        SearchPublicMatchResponse SearchPublicMatch(SearchPublicMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse SubscribeLobby(SubscribeLobbyRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse UnsubscribeLobby(UnsubscribeLobbyRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse SendMatchInvitation(SendMatchInvitationRequest request);
    }
}