using System.Collections.Generic;

namespace GuessWhoCore.Dtos
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
