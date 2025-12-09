using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface ILeaderboardService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        GetLeaderboardResponse GetGlobalLeaderboard(GetLeaderboardRequest request);
    }
}