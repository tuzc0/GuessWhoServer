using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface ICharacterRepository
    {
        IReadOnlyList<string> GetActiveCharacterIds();
    }

    public interface IMatchDeckRepository
    {
        MatchDeckResult GetMatchDeck(long matchId);
        MatchDeckResult CreateDeck(SaveMatchDeckArgs saveMatchDeckArgs);
    }
}
