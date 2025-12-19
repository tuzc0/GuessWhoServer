using ClassLibraryGuessWho.Data.DataAccess.Characters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;

namespace WcfServiceLibraryGuessWho.Services.MatchApplication
{
    public sealed class MatchDeckProvider : IMatchDeckProvider
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchDeckProvider));

        private const long INVALID_MATCH_ID = 0;
        private const int MINIMUM_CHARACTERS_REQUIRED = 1;
        private const int MINIMUM_SHUFFLE_INDEX = 1;

        private const int MINIMUM_DECK_SIZE = 4;
        private const int MAXIMUM_DECK_SIZE = 60;
        private const int DEFAULT_DECK_SIZE = 24;

        private const int SQL_ERROR_PRIMARY_KEY_VIOLATION = 2627;

        private const string ERROR_INVALID_MATCH_ID = "InvalidMatchId";
        private const string ERROR_DECK_GENERATION = "DeckGenerationFailed";
        private const string ERROR_DECK_PERSISTENCE = "DeckPersistenceFailed";
        private const string ERROR_UNKNOWN = "UnknownError";

        private const string MESSAGE_INVALID_MATCH_ID = "The match identifier is invalid.";
        private const string MESSAGE_DECK_GENERATION_FAILED = "The character deck could not be generated.";
        private const string MESSAGE_DECK_PERSISTENCE_FAILED = "The character deck could not be stored.";
        private const string MESSAGE_UNKNOWN_ERROR = "An unexpected error occurred while processing the match deck.";

        private readonly CharacterData characterData;
        private readonly CharacterDeckData characterDeckData;

        public MatchDeckProvider(CharacterData characterData, CharacterDeckData characterDeckData)
        {
            this.characterDeckData = characterDeckData ?? 
                throw new ArgumentNullException(nameof(characterDeckData));
            this.characterData = characterData ?? 
                throw new ArgumentNullException(nameof(characterData));
        }

        public string[] CreateDeck(long matchId, int requestedDeckSize)
        {
            if (matchId <= INVALID_MATCH_ID)
            {
                throw Faults.Create(
                    ERROR_INVALID_MATCH_ID,
                    MESSAGE_INVALID_MATCH_ID);
            }

            try
            {
                string[] generatedDeckCharacterIds = GenerateDeckForMatch(matchId, requestedDeckSize);

                if (generatedDeckCharacterIds.Length < MINIMUM_CHARACTERS_REQUIRED)
                {
                    Logger.WarnFormat("CreateDeck: no characters available to generate deck. MatchId={0}",
                        matchId);
                    throw Faults.Create(
                        ERROR_DECK_GENERATION,
                        MESSAGE_DECK_GENERATION_FAILED);
                }

                var deckDto = new CharacterDeckDto
                {
                    MatchId = matchId,
                    CharacterDeckIds = generatedDeckCharacterIds.ToList()
                };

                bool wasDeckSaved;

                try
                {
                    wasDeckSaved = characterDeckData.SaveCharacterDeck(deckDto);
                }
                catch (DbUpdateException ex)
                {
                    var sqlException = ex.InnerException?.InnerException as SqlException;

                    if (sqlException != null &&
                        sqlException.Number == SQL_ERROR_PRIMARY_KEY_VIOLATION)
                    {
                        Logger.WarnFormat(
                            "CreateDeck: PK violation, deck was probably created concurrently. MatchId={0}",
                            matchId);

                        CharacterDeckDto existingDeck = characterDeckData.GetCharacterDeck(matchId);

                        if (existingDeck != null && existingDeck.IsValid)
                        {
                            return existingDeck.CharacterDeckIds.ToArray();
                        }
                    }

                    throw;
                }

                if (!wasDeckSaved)
                {
                    Logger.WarnFormat(
                        "CreateDeck: SaveCharacterDeck returned false. MatchId={0}",
                        matchId);

                    throw Faults.Create(
                        ERROR_DECK_PERSISTENCE,
                        MESSAGE_DECK_PERSISTENCE_FAILED);
                }

                Logger.InfoFormat(
                    "CreateDeck: deck created and stored successfully. MatchId={0}, Count={1}",
                    matchId,
                    generatedDeckCharacterIds.Length);

                return generatedDeckCharacterIds;
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception exception)
            {
                Logger.Fatal(
                    string.Format(
                        "CreateDeck: unexpected error. MatchId={0}",
                        matchId),
                    exception);

                throw Faults.Create(
                    ERROR_UNKNOWN,
                    MESSAGE_UNKNOWN_ERROR);
            }
        }

        public string[] GetMatchDeck(long matchId, int requestedDeckSize)
        {
            if (matchId <= INVALID_MATCH_ID)
            {
                throw Faults.Create(
                    ERROR_INVALID_MATCH_ID,
                    MESSAGE_INVALID_MATCH_ID);
            }

            try
            {
                CharacterDeckDto existingDeck = characterDeckData.GetCharacterDeck(matchId);

                if (existingDeck.IsValid)
                {
                    int effectiveDeckSize = GetEffectiveDeckSize(requestedDeckSize,
                        existingDeck.CharacterDeckIds.Count);

                    return existingDeck
                        .CharacterDeckIds
                        .Take(effectiveDeckSize)
                        .ToArray();
                }

                return CreateDeck(matchId, requestedDeckSize);
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception exception)
            {
                Logger.Fatal(string.Format("GetMatchDeck: unexpected error. MatchId={0}",
                        matchId), exception);
                throw Faults.Create(
                    ERROR_UNKNOWN,
                    MESSAGE_UNKNOWN_ERROR);
            }
        }

        private string[] GenerateDeckForMatch(long matchId, int requestedDeckSize)
        {
            var activeCharacters = characterData.GetActiveCharacters();

            if (activeCharacters.Count < MINIMUM_CHARACTERS_REQUIRED)
            {
                Logger.Warn("GenerateDeckForMatch: no active characters found.");
                return Array.Empty<string>();
            }

            var deckCharacterIds = activeCharacters
                .Where(character => character != null && character.IsValid && character.IsActive)
                .Select(character => character.CharacterId)
                .ToList();

            if (deckCharacterIds.Count < MINIMUM_CHARACTERS_REQUIRED)
            {
                Logger.Warn("GenerateDeckForMatch: no characters passed the filter criteria.");
                return Array.Empty<string>();
            }

            int effectiveDeckSize = GetEffectiveDeckSize(requestedDeckSize, deckCharacterIds.Count);

            var randomNumberGenerator = new Random(unchecked((int)(DateTime.UtcNow.Ticks ^ matchId)));

            for (int index = deckCharacterIds.Count - 1; index >= MINIMUM_SHUFFLE_INDEX; index--)
            {
                int randomUpperBoundExclusive = index + 1;
                int swapIndex = randomNumberGenerator.Next(randomUpperBoundExclusive);

                string temporaryCharacterId = deckCharacterIds[index];
                deckCharacterIds[index] = deckCharacterIds[swapIndex];
                deckCharacterIds[swapIndex] = temporaryCharacterId;
            }

            deckCharacterIds = deckCharacterIds.Take(effectiveDeckSize).ToList();

            string[] deckCharacterIdArray = deckCharacterIds.ToArray();

            return deckCharacterIdArray;
        }

        private int GetEffectiveDeckSize(int requestedDeckSize, int availableCharactersCount)
        {
            int normalizedRequestedSize = requestedDeckSize;

            if (normalizedRequestedSize <= 0)
            {
                normalizedRequestedSize = DEFAULT_DECK_SIZE;
            }

            if (normalizedRequestedSize < MINIMUM_DECK_SIZE)
            {
                normalizedRequestedSize = MINIMUM_DECK_SIZE;
            }

            if (normalizedRequestedSize > MAXIMUM_DECK_SIZE)
            {
                normalizedRequestedSize = MAXIMUM_DECK_SIZE;
            }

            if (normalizedRequestedSize > availableCharactersCount)
            {
                normalizedRequestedSize = availableCharactersCount;
            }

            return normalizedRequestedSize;
        }
    }
}
