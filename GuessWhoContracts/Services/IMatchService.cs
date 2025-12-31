using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IMatchCallback))]
    public interface IMatchService
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
        BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse StartMatch(StartMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse EndMatch(EndMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        MatchDeckResponse GetMatchDeck(GetOrCreateMatchDeckRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void SubscribeLobby(long matchId);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void UnsubscribeLobby(long matchId);
    }
}
