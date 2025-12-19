using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public class CharacterDto
    {
        public const string INVALID_CHARACTER_ID = "00000";

        public string CharacterId { get; set; }
        public bool IsActive { get; set; }
        public bool IsValid => CharacterId != INVALID_CHARACTER_ID;
   
        public static CharacterDto CreateInvalid()
        {
            return new CharacterDto
            {
                CharacterId = INVALID_CHARACTER_ID,
                IsActive = false
            };
        }
    }
}
