using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Characters
{
    public sealed class CharacterDeckData : IMatchDeckRepository
    {
        private const int FIRST_POSITION = 1;

        private readonly GuessWhoDBEntities dataContext;

        public CharacterDeckData(GuessWhoDBEntities context)
        {
            dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public MatchDeckRecord GetMatchDeck(long matchId)
        {
            if (matchId <= 0)
            {
                return MatchDeckRecord.CreateInvalid();
            }

            List<string> characterIds = dataContext.MATCH_DECK_CARD
                .AsNoTracking()
                .Where(deckCard => deckCard.MATCHID == matchId)
                .OrderBy(deckCard => deckCard.POSITION)
                .Select(deckCard => deckCard.CHARACTERID)
                .Where(characterId => characterId != null)
                .ToList();

            if (characterIds.Count == 0)
            {
                return MatchDeckRecord.CreateInvalid();
            }

            return new MatchDeckRecord(matchId, characterIds);
        }

        public MatchDeckRecord CreateDeck(SaveMatchDeckArgs saveMatchDeckArgs)
        {
            if (saveMatchDeckArgs == null || saveMatchDeckArgs.MatchId <= 0)
            {
                return MatchDeckRecord.CreateInvalid();
            }

            List<string> cleanedIds = (saveMatchDeckArgs.CharacterIds ?? Array.Empty<string>())
                .Where(characterId => !string.IsNullOrWhiteSpace(characterId))
                .Select(characterId => characterId.Trim())
                .ToList();

            if (cleanedIds.Count == 0)
            {
                return MatchDeckRecord.CreateInvalid();
            }

            bool deckAlreadyExists = dataContext.MATCH_DECK_CARD
                .AsNoTracking()
                .Any(deckCard => deckCard.MATCHID == saveMatchDeckArgs.MatchId);

            if (deckAlreadyExists)
            {
                return MatchDeckRecord.CreateInvalid();
            }

            int position = FIRST_POSITION;

            foreach (string characterId in cleanedIds)
            {
                var entity = new MATCH_DECK_CARD
                {
                    MATCHID = saveMatchDeckArgs.MatchId,
                    POSITION = position,
                    CHARACTERID = characterId
                };

                dataContext.MATCH_DECK_CARD.Add(entity);
                position++;
            }

            return new MatchDeckRecord(saveMatchDeckArgs.MatchId, cleanedIds);
        }
    }
}
