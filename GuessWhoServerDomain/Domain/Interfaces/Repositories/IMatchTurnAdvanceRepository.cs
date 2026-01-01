using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchTurnAdvanceRepository
    {
        AdvanceTurnResult AdvanceTurn(AdvanceTurnArgs turnArgs);
    }
}
