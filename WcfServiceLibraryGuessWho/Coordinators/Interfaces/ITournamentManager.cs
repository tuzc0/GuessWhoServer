using GuessWhoDataAccess.Data;
using GuessWhoServerDomain.Domain.Enums.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoServices.Coordinators.Interfaces
{
    [ServiceContract]
    public interface ITournamentManager
    {
        [OperationContract]
        long CreateTournament(long hostUserId, int turnSeconds);

        [OperationContract]
        bool JoinTournament(long tournamentId, long userId);

        [OperationContract]
        TournamentStatus GetTournamentStatus(long tournamentId);
    }
}
