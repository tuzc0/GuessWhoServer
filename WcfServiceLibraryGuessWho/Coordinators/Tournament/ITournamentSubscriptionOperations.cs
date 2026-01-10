using GuessWhoContracts.Services;
using GuessWhoCore.Contracts.Response;
using GuessWhoServices.Coordinators.InternalDtos;
using System.Collections.Generic;

namespace GuessWhoServices.Coordinators.Tournament
{
    public interface ITournamentSubscriptionOperations
    {
        bool Subscribe(TournamentSubscriptionArgs args);
        bool Unsubscribe(TournamentSubscriptionArgs args);
        void NotifyTournamentLobbyUpdated(long tournamentId, IEnumerable<TournamentPlayerDto> players);
        void NotifyTournamentStarted(long tournamentId, long match1Id, long match2Id);
        void NotifyTournamentFinalStarted(long tournamentId, long finalMatchId);
    }
}