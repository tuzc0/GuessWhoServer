using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Characters
{
    public sealed class CharacterDeckData : IMatchDeckRepository
    {
        private const int FIRST_POSITION = 1;

        private readonly GuessWhoDBEntities dataContext;

        public CharacterDeckData(GuessWhoDBEntities context)
        {
            dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public MatchDeckResult GetMatchDeck(long matchId)
        {
            if (matchId <= 0)
            {
                return MatchDeckResult.Fail(MatchDeckResultCode.InvalidMatchId);
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
                return MatchDeckResult.Fail(MatchDeckResultCode.DeckNotFound);
            }

            return MatchDeckResult.Success(matchId, characterIds);
        }

        public MatchDeckResult CreateDeck(SaveMatchDeckArgs saveMatchDeckArgs)
        {
            if (saveMatchDeckArgs == null || saveMatchDeckArgs.MatchId <= 0)
            {
                return MatchDeckResult.Fail(MatchDeckResultCode.InvalidArgs);
            }

            List<string> cleanedIds = (saveMatchDeckArgs.CharacterIds ?? Array.Empty<string>())
                .Where(characterId => !string.IsNullOrWhiteSpace(characterId))
                .Select(characterId => characterId.Trim())
                .ToList();

            if (cleanedIds.Count == 0)
            {
                return MatchDeckResult.Fail(MatchDeckResultCode.InvalidArgs);
            }

            bool deckAlreadyExists = dataContext.MATCH_DECK_CARD
                .AsNoTracking()
                .Any(deckCard => deckCard.MATCHID == saveMatchDeckArgs.MatchId);

            if (deckAlreadyExists)
            {
                return MatchDeckResult.Fail(MatchDeckResultCode.DeckAlreadyExists);
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

            return MatchDeckResult.Success(saveMatchDeckArgs.MatchId, cleanedIds);
        }
    }
}
