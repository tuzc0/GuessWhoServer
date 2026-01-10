using GuessWhoCore.Contracts.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface ITournamentCallback : IMatchCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnTournamentLobbyUpdated(IEnumerable<TournamentPlayerDto> players);

        [OperationContract(IsOneWay = true)]
        void OnTournamentStarted(long match1Id, long match2Id);

        [OperationContract(IsOneWay = true)]
        void OnTournamentFinalStarted(long finalMatchId);
    }
}
