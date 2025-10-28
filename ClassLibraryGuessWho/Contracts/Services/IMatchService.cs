using ClassLibraryGuessWho.Contracts.Dtos;
using System.ServiceModel;

namespace ClassLibraryGuessWho.Contracts.Services
{
    [ServiceContract]
    public interface IMatchService
    {
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
