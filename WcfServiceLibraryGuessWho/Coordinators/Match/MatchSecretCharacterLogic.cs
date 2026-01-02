using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using GuessWhoServices.Infrastructure;
using log4net;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GuessWhoServices.Coordinators.Match
{
    public sealed class MatchSecretCharacterLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchSecretCharacterLogic));

        private const long INVALID_ID = 0;
        private const int TURN_PLAYERS_REQUIRED = 2;

        private const int TURN_INIT_VERIFY_ATTEMPTS = 2;

        private const int RANDOM_BYTE_COUNT = 1;
        private const int RANDOM_BIT_MASK = 1;

        private const string CONTEXT_TURN_INIT = "MatchSecretCharacterLogic.TryInitializeFirstTurnIfReady";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IMatchCallbackDispatcher callbackDispatcher;

        private enum TurnInitOutcome
        {
            NotNeeded = 0,
            SuccessOrAlreadyInitialized = 1,
            Failed = 2
        }

        private readonly record struct RandomBoolResult(bool IsSuccess, bool Value)
        {
            public static RandomBoolResult Success(bool value) => new(true, value);
            public static RandomBoolResult Fail() => new(false, false);
        }

        public MatchSecretCharacterLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IMatchCallbackDispatcher callbackDispatcher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.callbackDispatcher = callbackDispatcher ?? 
                throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs secretCharacterArgs)
        {
            if (secretCharacterArgs == null)
            {
                throw new ArgumentNullException(nameof(secretCharacterArgs));
            }

            if (secretCharacterArgs.MatchId <= INVALID_ID)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotFound);
            }

            if (secretCharacterArgs.UserProfileId <= INVALID_ID)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerNotInMatch);
            }

            string trimmedCharacterId = (secretCharacterArgs.SecretCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedCharacterId))
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.InvalidCharacter);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            ChooseSecretCharacterResult chooseResult = unitOfWork.Matches.ChooseSecretCharacter(
                new ChooseSecretCharacterArgs
                {
                    MatchId = secretCharacterArgs.MatchId,
                    UserProfileId = secretCharacterArgs.UserProfileId,
                    SecretCharacterId = trimmedCharacterId
                }
            );

            if (!chooseResult.IsSuccess)
            {
                transaction.Rollback();

                return chooseResult;
            }

            bool areAllChosen = unitOfWork.Matches.AreAllSecretCharactersChosen(secretCharacterArgs.MatchId);

            if (areAllChosen)
            {
                TurnInitOutcome turnInitOutcome = TurnInitOutcome.NotNeeded;

                turnInitOutcome = TryInitializeFirstTurnIfReady(unitOfWork, secretCharacterArgs.MatchId);

                if (turnInitOutcome == TurnInitOutcome.Failed)
                {
                    Logger.WarnFormat("{0}: turn init failed after all secrets chosen. matchId={1}. Rolling back.",
                        CONTEXT_TURN_INIT, secretCharacterArgs.MatchId);

                    transaction.Rollback();

                    return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.OperationConflict);
                }
            }

            unitOfWork.Flush();
            transaction.Commit();

            callbackDispatcher.Broadcast(
                secretCharacterArgs.MatchId,
                callback => callback.OnSecretCharacterChosen(secretCharacterArgs.MatchId, secretCharacterArgs.UserProfileId));

            if (areAllChosen)
            {
                callbackDispatcher.Broadcast(
                    secretCharacterArgs.MatchId,
                    callback => callback.OnAllSecretCharactersChosen(secretCharacterArgs.MatchId));
            }

            return ChooseSecretCharacterResult.Success();
        }

        public ChangeSecretCharacterResult ChangeSecretCharacter(ChangeSecretCharacterArgs changeCharacterArgs)
        {
            if (changeCharacterArgs == null)
            {
                throw new ArgumentNullException(nameof(changeCharacterArgs));
            }

            if (changeCharacterArgs.MatchId <= INVALID_ID)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotFound);
            }

            if (changeCharacterArgs.UserId <= INVALID_ID)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerNotInMatch);
            }

            string trimmedCharacterId = (changeCharacterArgs.SecretCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedCharacterId))
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.InvalidCharacter);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
            using IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction();

            ChangeSecretCharacterResult changeResult = unitOfWork.Matches.ChangeSecretCharacter(
                new ChangeSecretCharacterArgs( 
                    changeCharacterArgs.MatchId, 
                    changeCharacterArgs.UserId, 
                    trimmedCharacterId)
            );

            if (!changeResult.IsSuccess)
            {
                transaction.Rollback();
                return changeResult;
            }

            unitOfWork.Flush();
            transaction.Commit();

            callbackDispatcher.Broadcast(
                changeCharacterArgs.MatchId,
                callback => callback.OnSecretCharacterChosen(changeCharacterArgs.MatchId, changeCharacterArgs.UserId));

            return ChangeSecretCharacterResult.Success();
        }

        private TurnInitOutcome TryInitializeFirstTurnIfReady(IGuessWhoUnitOfWork unitOfWork, long matchId)
        {
            if (unitOfWork == null || matchId <= INVALID_ID)
            {
                return TurnInitOutcome.Failed;
            }

            TurnStateSnapshot existingTurnState = unitOfWork.MatchesTurns.GetTurnState(matchId);

            if (existingTurnState != null && existingTurnState.MatchId > INVALID_ID)
            {
                return TurnInitOutcome.SuccessOrAlreadyInitialized;
            }

            IReadOnlyList<long> activePlayerUserIds =
                unitOfWork.Matches.GetActivePlayerIds(matchId, TURN_PLAYERS_REQUIRED) ?? Array.Empty<long>();

            if (activePlayerUserIds.Count != TURN_PLAYERS_REQUIRED)
            {
                return TurnInitOutcome.Failed;
            }

            long firstCandidateUserId = activePlayerUserIds[0];
            long secondCandidateUserId = activePlayerUserIds[1];

            if (firstCandidateUserId <= INVALID_ID || secondCandidateUserId <= INVALID_ID ||
                firstCandidateUserId == secondCandidateUserId)
            {
                return TurnInitOutcome.Failed;
            }

            RandomBoolResult randomStartDecision = TryGetRandomBool();
            bool shouldFirstCandidateStart = !randomStartDecision.IsSuccess || randomStartDecision.Value;

            long startingPlayerUserId = shouldFirstCandidateStart ? firstCandidateUserId : secondCandidateUserId;
            long nonStartingPlayerUserId = shouldFirstCandidateStart ? secondCandidateUserId : firstCandidateUserId;

            var initializeTurnOrderArgs = new InitializeTurnOrderArgs
            {
                MatchId = matchId,
                UserIdsInOrder = new List<long>(TURN_PLAYERS_REQUIRED) { startingPlayerUserId, nonStartingPlayerUserId },
                NowUtc = DateTime.UtcNow
            };

            InitializeTurnOrderResult initResult = unitOfWork.MatchesTurns.InitializeTurnOrder(initializeTurnOrderArgs);

            if (initResult.IsSuccess ||
                initResult.Code == InitializeTurnOrderResultCode.AlreadyInitialized)
            {
                return TurnInitOutcome.SuccessOrAlreadyInitialized;
            }

            if (initResult.Code == InitializeTurnOrderResultCode.OperationConflict)
            {
                for (int attempt = 0; attempt < TURN_INIT_VERIFY_ATTEMPTS; attempt++)
                {
                    TurnStateSnapshot state = unitOfWork.MatchesTurns.GetTurnState(matchId);

                    if (state != null && state.MatchId > INVALID_ID)
                    {
                        return TurnInitOutcome.SuccessOrAlreadyInitialized; 
                    }
                }

                Logger.WarnFormat("{0}: init turn conflict but state not found after verification. matchId={1}.",
                    CONTEXT_TURN_INIT, matchId);

                return TurnInitOutcome.Failed;
            }

            Logger.WarnFormat("{0}: init turn failed. matchId={1} code={2}.", CONTEXT_TURN_INIT,
                matchId, initResult.Code);

            return TurnInitOutcome.Failed;
        }

        private static RandomBoolResult TryGetRandomBool()
        {
            try
            {
                byte[] randomByteBuffer = new byte[RANDOM_BYTE_COUNT];

                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomByteBuffer);
                }

                bool generatedValue = (randomByteBuffer[0] & RANDOM_BIT_MASK) == RANDOM_BIT_MASK;
                return RandomBoolResult.Success(generatedValue);
            }
            catch (CryptographicException)
            {
                return RandomBoolResult.Fail();
            }
            catch (PlatformNotSupportedException)
            {
                return RandomBoolResult.Fail();
            }
        }
    }
}
