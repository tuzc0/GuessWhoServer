using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Characters
{
    public class CharacterData : ICharacterRepository
    {
        private const string EMPTY = "";

        private readonly GuessWhoDBEntities dataContext;

        public CharacterData(GuessWhoDBEntities context)
        {
            dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IReadOnlyList<string> GetActiveCharacterIds()
        {
            return dataContext.CHARACTER
                .Where(c => c.ISACTIVE)
                .Select(c => c.CHARACTERID)
                .Where(characterId => characterId != null && characterId.Trim() != EMPTY)
                .ToList();
        }
    }
}
