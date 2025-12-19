using GuessWhoContracts.Dtos.Dto;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Characters
{
    public class CharacterData
    {
        public List<CharacterDto> GetActiveCharacters()
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.CHARACTER
                    .Where(a => a.ISACTIVE)
                    .Select(a => new CharacterDto
                    {
                        CharacterId = a.CHARACTERID,
                        IsActive = a.ISACTIVE
                    })
                    .ToList();
            }
        }
    }
}
