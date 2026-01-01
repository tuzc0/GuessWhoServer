using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchTurnRepository
    {
        bool HasTurnState(long matchId);
        InitializeTurnOrderResult InitializeTurnOrder(InitializeTurnOrderArgs orderArgs);
        TurnStateSnapshot GetTurnState(long matchId);
    }
}
