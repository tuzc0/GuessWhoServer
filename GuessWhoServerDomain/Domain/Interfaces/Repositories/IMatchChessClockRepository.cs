using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchChessClockRepository
    {
        ApplyChessClockResult ApplyOnPassTurn(ApplyChessClockArgs chessClockArgs);

        MatchClockSnapshot GetPlayerClockState(long matchId, long userId);
    }
}
