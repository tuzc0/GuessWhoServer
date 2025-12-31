using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface ICharacterRepository
    {
        IReadOnlyList<string> GetActiveCharacterIds();
    }

    public interface IMatchDeckRepository
    {
        MatchDeckRecord GetMatchDeck(long matchId);
        MatchDeckRecord CreateDeck(SaveMatchDeckArgs saveMatchDeckArgs);
    }
}
