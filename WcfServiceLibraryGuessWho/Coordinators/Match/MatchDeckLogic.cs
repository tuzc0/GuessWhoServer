using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Faults.Match;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Security.Cryptography;
using WcfServiceLibraryGuessWho.Errors;
using WcfServiceLibraryGuessWho.Services.MatchApplication;

namespace WcfServiceLibraryGuessWho.Coordinators.Match
{
    public sealed class MatchDeckLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchDeckLogic));

        private const string CONTEXT_GET_OR_CREATE = "MatchDeckLogic.GetOrCreateMatchDeck";
        private const long INVALID_MATCH_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public MatchDeckLogic(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public MatchDeckResponse GetOrCreateMatchDeck(GetOrCreateMatchDeckRequest args)
        {
            ValidateArgsOrThrow(args);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            MatchDeckRecord existingDeck = SafeGetDeck(unitOfWork, args.MatchId);

            if (existingDeck.IsValid)
            {
                return BuildResponse(existingDeck);
            }

            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            MatchDeckRecord recheckedDeck = SafeGetDeck(unitOfWork, args.MatchId);

            if (recheckedDeck.IsValid)
            {
                transaction.Commit();
                return BuildResponse(recheckedDeck);
            }

            int deckSize = ResolveDeckSize(args.ModeId);

            List<string> candidates = LoadCandidateCharacters(unitOfWork);
            EnsureEnoughCharactersOrThrow(args.MatchId, deckSize, candidates.Count);

            List<string> selected = SelectRandomCharacters(candidates, deckSize);

            var saveArgs = new SaveMatchDeckArgs
            {
                MatchId = args.MatchId,
                CharacterIds = selected
            };

            try
            {
                unitOfWork.MatchDecks.CreateDeck(saveArgs);

                unitOfWork.Flush();
                transaction.Commit();
            }
            catch (DbUpdateException ex) when (FaultTranslator.IsUniqueConstraintViolation(ex))
            {
                transaction.Rollback();

                Logger.InfoFormat("{0}: deck already created by concurrent request. MatchId={1}. Returning existing deck.",
                    CONTEXT_GET_OR_CREATE, args.MatchId);

                MatchDeckRecord deckExisting = SafeGetDeck(unitOfWork, args.MatchId);

                if (deckExisting.IsValid)
                {
                    return BuildResponse(deckExisting);
                }

                throw FaultsFactory.Create(
                    MatchDeckFaultKeys.CODE_OPERATION_CONFLICT,
                    MatchDeckFaultKeys.MSG_OPERATION_CONFLICT,
                    MatchDeckFaultKeys.FALLBACK_OPERATION_CONFLICT,
                    ex);
            }

            MatchDeckRecord createdDeck = SafeGetDeck(unitOfWork, args.MatchId);

            if (createdDeck.IsValid)
            {
                return BuildResponse(createdDeck);
            }

            throw FaultsFactory.Create(
                MatchDeckFaultKeys.CODE_DECK_GENERATION_FAILED,
                MatchDeckFaultKeys.MSG_DECK_GENERATION_FAILED,
                MatchDeckFaultKeys.FALLBACK_DECK_GENERATION_FAILED);
        }

        private static void ValidateArgsOrThrow(GetOrCreateMatchDeckRequest args)
        {
            if (args == null)
            {
                throw FaultsFactory.Create(
                    MatchDeckFaultKeys.CODE_INVALID_REQUEST,
                    MatchDeckFaultKeys.MSG_INVALID_REQUEST,
                    MatchDeckFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            if (args.MatchId <= INVALID_MATCH_ID)
            {
                throw FaultsFactory.Create(
                    MatchDeckFaultKeys.CODE_INVALID_MATCH_ID,
                    MatchDeckFaultKeys.MSG_INVALID_MATCH_ID,
                    MatchDeckFaultKeys.FALLBACK_INVALID_MATCH_ID);
            }
        }

        private static MatchDeckRecord SafeGetDeck(IGuessWhoUnitOfWork unitOfWork, long matchId)
        {
            MatchDeckRecord record = unitOfWork.MatchDecks.GetMatchDeck(matchId);

            if (record == null)
            {
                return MatchDeckRecord.CreateInvalid();
            }

            return record;
        }

        private static MatchDeckResponse BuildResponse(MatchDeckRecord record)
        {
            string[] characterIds = record.CharacterDeckIds != null
                ? record.CharacterDeckIds.ToArray()
                : Array.Empty<string>();

            return new MatchDeckResponse { CharacterIds = characterIds };
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
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void EnsureEnoughCharactersOrThrow(long matchId, int required, int available)
        {
            if (available >= required)
            {
                return;
            }

            Logger.WarnFormat("{0}: insufficient active characters. MatchId={1}, Required={2}, Available={3}.",
                CONTEXT_GET_OR_CREATE, matchId, required, available);

            throw FaultsFactory.Create(
                MatchDeckFaultKeys.CODE_INSUFFICIENT_CHARACTERS,
                MatchDeckFaultKeys.MSG_INSUFFICIENT_CHARACTERS,
                MatchDeckFaultKeys.FALLBACK_INSUFFICIENT_CHARACTERS);
        }

        private static List<string> SelectRandomCharacters(List<string> candidates, int deckSize)
        {
            if (candidates == null || candidates.Count == 0)
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

            using (RandomNumberGenerator randomNumber = RandomNumberGenerator.Create())
            {
                for (int index = items.Count - 1; index > 0; index--)
                {
                    int swapIndex = GetRandomInt(randomNumber, index + 1);

                    string temp = items[index];
                    items[index] = items[swapIndex];
                    items[swapIndex] = temp;
                }
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

            byte[] buffer = new byte[4];

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
