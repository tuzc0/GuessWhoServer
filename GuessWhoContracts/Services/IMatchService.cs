using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract(CallbackContract = typeof(IMatchCallback))]
    public interface IMatchService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void SusbcribeLobby(long matchId);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void UnsusbcribeLobby(long matchId);

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
    }
}
