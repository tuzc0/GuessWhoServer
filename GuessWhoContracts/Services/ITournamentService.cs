using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Faults;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface ITournamentService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        CreateTournamentResponse CreateTournament(CreateTournamentRequest request);
    }

}
