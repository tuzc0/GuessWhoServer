using System.Collections.Generic;

namespace GuessWhoContracts.Dtos.Dto
{
    public class CharacterDeckDto
    {
        public long MatchId { get; set; }
        public List<string> CharacterDeckIds { get; set; }

        public bool IsValid =>
            CharacterDeckIds != null &&
            CharacterDeckIds.Count > 0;

        public static CharacterDeckDto CreateInvalid()
        {
            return new CharacterDeckDto
            {
                CharacterDeckIds = new List<string>() 
            };
        }
    }
}
