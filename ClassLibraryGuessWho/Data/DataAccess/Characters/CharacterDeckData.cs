using GuessWhoContracts.Dtos.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Characters
{
    public sealed class CharacterDeckData
    {
        private const int FIRST_POSITION = 1;

        public CharacterDeckDto GetCharacterDeck(long matchId)
        {
            using (var dataContext = new GuessWhoDBEntities())
            {
                List<string> characterIds = dataContext.MATCH_DECK_CARD
                    .Where(deckCard => deckCard.MATCHID == matchId)
                    .OrderBy(deckCard => deckCard.POSITION)
                    .Select(deckCard => deckCard.CHARACTERID)
                    .ToList();

                if (characterIds.Count == 0)
                {
                    return CharacterDeckDto.CreateInvalid();
                }

                return new CharacterDeckDto
                {
                    MatchId = matchId,                      
                    CharacterDeckIds = characterIds
                };
            }
        }

        public bool SaveCharacterDeck(CharacterDeckDto deck)
        {
            if (deck == null || !deck.IsValid)
            {
                return false;
            }

            using (var dataContext = new GuessWhoDBEntities())
            {
                var existingCards = dataContext.MATCH_DECK_CARD
                    .Where(d => d.MATCHID == deck.MatchId)
                    .ToList();

                if (existingCards.Count > 0)
                {
                    dataContext.MATCH_DECK_CARD.RemoveRange(existingCards);
                }

                int position = FIRST_POSITION;

                foreach (string characterId in deck.CharacterDeckIds)
                {
                    if (string.IsNullOrWhiteSpace(characterId))
                    {
                        continue;
                    }

                    var entity = new MATCH_DECK_CARD
                    {
                        MATCHID = deck.MatchId,
                        POSITION = position,
                        CHARACTERID = characterId
                    };

                    dataContext.MATCH_DECK_CARD.Add(entity);
                    position++;
                }

                int affectedRows = dataContext.SaveChanges();

                return affectedRows > 0;
            }
        }
    }
}
