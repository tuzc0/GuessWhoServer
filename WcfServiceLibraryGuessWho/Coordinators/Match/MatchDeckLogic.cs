using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Requests;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Security.Cryptography;
using WcfServiceLibraryGuessWho.Errors;
using WcfServiceLibraryGuessWho.Services.MatchApplication;

namespace GuessWhoServices.Services.MatchApplication
{
    public sealed class MatchDeckLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchDeckLogic));

        private const string CONTEXT_GET_OR_CREATE = "MatchDeckLogic.GetOrCreateMatchDeck";

        private const long INVALID_MATCH_ID = 0;
        private const int RANDOM_INT_BUFFER_LENGTH = 4;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public MatchDeckLogic(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public MatchDeckResult GetOrCreateMatchDeck(GetOrCreateMatchDeckRequest createDeckRequest)
        {
            if (createDeckRequest == null)
            {
                return MatchDeckResult.Fail(MatchDeckResultCode.InvalidArgs);
            }

            if (createDeckRequest.MatchId <= INVALID_MATCH_ID)
            {
                return MatchDeckResult.Fail(MatchDeckResultCode.InvalidMatchId);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            MatchDeckResult existingDeck = unitOfWork.MatchDecks.GetMatchDeck(createDeckRequest.MatchId);

            if (existingDeck.IsSuccess)
            {
                return existingDeck;
            }

            int deckSize = ResolveDeckSize(createDeckRequest.ModeId);

            List<string> candidates = LoadCandidateCharacters(unitOfWork);

            if (candidates.Count < deckSize)
            {
                Logger.WarnFormat("{0}: insufficient active characters. MatchId={1}, Required={2}, Available={3}.",
                    CONTEXT_GET_OR_CREATE, createDeckRequest.MatchId, deckSize, candidates.Count);

                return MatchDeckResult.Fail(MatchDeckResultCode.InsufficientCharacters);
            }

            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            MatchDeckResult recheckedDeck = unitOfWork.MatchDecks.GetMatchDeck(createDeckRequest.MatchId);

            if (recheckedDeck.IsSuccess)
            {
                transaction.Commit();
                return recheckedDeck;
            }

            List<string> selected = SelectRandomCharacters(candidates, deckSize);

            var saveArgs = new SaveMatchDeckArgs
            {
                MatchId = createDeckRequest.MatchId,
                CharacterIds = selected
            };

            MatchDeckResult createResult = unitOfWork.MatchDecks.CreateDeck(saveArgs);

            if (!createResult.IsSuccess && createResult.Code == MatchDeckResultCode.DeckAlreadyExists)
            {
                transaction.Rollback();
                return ReloadDeckAfterConflict(createDeckRequest.MatchId);
            }

            if (!createResult.IsSuccess)
            {
                transaction.Rollback();
                return createResult;
            }

            try
            {
                unitOfWork.Flush();
                transaction.Commit();
            }
            catch (DbUpdateException ex) when (FaultTranslator.IsUniqueConstraintViolation(ex))
            {
                transaction.Rollback();

                Logger.InfoFormat("{0}: deck already created by concurrent request. MatchId={1}.",
                    CONTEXT_GET_OR_CREATE, createDeckRequest.MatchId);

                return ReloadDeckAfterConflict(createDeckRequest.MatchId);
            }

            MatchDeckResult createdDeck = unitOfWork.MatchDecks.GetMatchDeck(createDeckRequest.MatchId);

            if (createdDeck.IsSuccess)
            {
                return createdDeck;
            }

            return MatchDeckResult.Fail(MatchDeckResultCode.DeckGenerationFailed);
        }

        private MatchDeckResult ReloadDeckAfterConflict(long matchId)
        {
            using IGuessWhoUnitOfWork retryUnitOfWork = unitOfWorkFactory.Create();

            MatchDeckResult deck = retryUnitOfWork.MatchDecks.GetMatchDeck(matchId);

            if (deck.IsSuccess)
            {
                return deck;
            }

            return MatchDeckResult.Fail(MatchDeckResultCode.OperationConflict);
        }

        private static int ResolveDeckSize(byte modeId)
        {
            if (modeId == MatchDeckConstants.MODE_CLASSIC_ID)
            {
                return MatchDeckConstants.CLASSIC_DECK_SIZE;
            }

            if (modeId == MatchDeckConstants.MODE_TOURNAMENT_ID)
            {
                return MatchDeckConstants.TOURNAMENT_DECK_SIZE;
            }

            return MatchDeckConstants.CLASSIC_DECK_SIZE;
        }

        private static List<string> LoadCandidateCharacters(IGuessWhoUnitOfWork unitOfWork)
        {
            IReadOnlyList<string> universe = unitOfWork.Characters.GetActiveCharacterIds() ?? Array.Empty<string>();

            return universe
                .Where(characterId => !string.IsNullOrWhiteSpace(characterId))
                .Select(characterId => characterId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> SelectRandomCharacters(List<string> candidates, int deckSize)
        {
            if (candidates == null || candidates.Count == 0 || deckSize <= 0)
            {
                return new List<string>();
            }

            ShuffleInPlace(candidates);

            return candidates.Take(deckSize).ToList();
        }

        private static void ShuffleInPlace(List<string> items)
        {
            if (items == null || items.Count <= 1)
            {
                return;
            }

            using RandomNumberGenerator randomNumber = RandomNumberGenerator.Create();

            for (int index = items.Count - 1; index > 0; index--)
            {
                int swapIndex = GetRandomInt(randomNumber, index + 1);

                string temp = items[index];
                items[index] = items[swapIndex];
                items[swapIndex] = temp;
            }
        }

        private static int GetRandomInt(RandomNumberGenerator randomNumber, int maxExclusive)
        {
            if (randomNumber == null)
            {
                throw new ArgumentNullException(nameof(randomNumber));
            }

            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            byte[] buffer = new byte[RANDOM_INT_BUFFER_LENGTH];

            while (true)
            {
                randomNumber.GetBytes(buffer);

                int value = BitConverter.ToInt32(buffer, 0) & int.MaxValue;

                int remainder = value % maxExclusive;
                int limit = int.MaxValue - (int.MaxValue % maxExclusive);

                if (value < limit)
                {
                    return remainder;
                }
            }
        }
    }
}
