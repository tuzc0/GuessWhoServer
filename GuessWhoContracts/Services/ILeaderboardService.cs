using GuessWhoCore.Contracts.Faults; 
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
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