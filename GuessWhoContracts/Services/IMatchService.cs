using GuessWhoContracts.Dtos.RequestAndResponse;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract(CallbackContract = typeof(IMatchCallback))]
    public interface IMatchService
    {
        [OperationContract]
        void SusbcribeLobby(long matchId);

        [OperationContract]
        void UnsusbcribeLobby(long matchId);

        [OperationContract]
        CreateMatchResponse CreateMatch(CreateMatchRequest request);

        [OperationContract]
        JoinMatchResponse JoinMatch(JoinMatchRequest request);

        [OperationContract]
        BasicResponse LeaveMatch(LeaveMatchRequest request);

        [OperationContract]
        BasicResponse SetPlayerReadyStatus(SetPlayerReadyStatusRequest request);

        [OperationContract]
        BasicResponse StartMatch(StartMatchRequest request);
    }
}
