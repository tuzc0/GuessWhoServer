using GuessWhoCore.Contracts.Response;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract(CallbackContract = typeof(ITournamentCallback))]
    public interface ITournamentService
    {
        [OperationContract]
        BasicResponse HostTournament(long hostUserId, int turnSeconds);

        [OperationContract]
        BasicResponse JoinTournament(long tournamentId, long userId);

        [OperationContract]
        void SubscribeTournament(long tournamentId, long userId);

        [OperationContract]
        void UnsubscribeTournament(long tournamentId);
    }
}