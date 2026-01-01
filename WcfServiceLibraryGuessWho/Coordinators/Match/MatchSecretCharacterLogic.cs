using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Faults.Match;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GuessWhoServices.Services.MatchApplication
{
    public sealed class MatchSecretCharacterLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchSecretCharacterLogic));

        private const string CONTEXT_CHOOSE_SECRET = "MatchSecretCharacterLogic.ChooseSecretCharacter";
        private const string CONTEXT_CHANGE_SECRET = "MatchSecretCharacterLogic.ChangeSecretCharacter";
        private const string CONTEXT_TURN_INIT = "MatchSecretCharacterLogic.TryInitializeFirstTurnIfReady";

        private const long INVALID_ID = 0;
        private const int TURN_PLAYERS_REQUIRED = 2;

        private const int RANDOM_BYTE_COUNT = 1;
        private const int RANDOM_BIT_MASK = 1;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IMatchCallbackDispatcher callbackDispatcher;

        private readonly record struct FaultTriplet(string Code, string MessageKey, string Fallback);

        private readonly record struct RandomBoolResult(bool IsSuccess, bool Value)
        {
            public static RandomBoolResult Success(bool value) => new(true, value);
            public static RandomBoolResult Fail() => new(false, false);
        }

        private static readonly IReadOnlyDictionary<ChooseSecretCharacterResultCode, FaultTriplet> FaultChooseMap =
            new Dictionary<ChooseSecretCharacterResultCode, FaultTriplet>
            {
                { ChooseSecretCharacterResultCode.MatchNotFound,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND,
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND,
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND) },
                { ChooseSecretCharacterResultCode.MatchNotInProgress,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_IN_PROGRESS,
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_IN_PROGRESS,
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_IN_PROGRESS) },
                { ChooseSecretCharacterResultCode.PlayerNotInMatch,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_PLAYER_NOT_IN_MATCH,
                        MatchLifecycleFaultKeys.MSG_PLAYER_NOT_IN_MATCH,
                        MatchLifecycleFaultKeys.FALLBACK_PLAYER_NOT_IN_MATCH) },
                { ChooseSecretCharacterResultCode.PlayerAlreadyLeft,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_PLAYER_ALREADY_LEFT,
                        MatchLifecycleFaultKeys.MSG_PLAYER_ALREADY_LEFT,
                        MatchLifecycleFaultKeys.FALLBACK_PLAYER_ALREADY_LEFT) },
                { ChooseSecretCharacterResultCode.InvalidCharacter,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_INVALID_CHARACTER,
                        MatchLifecycleFaultKeys.MSG_INVALID_CHARACTER,
                        MatchLifecycleFaultKeys.FALLBACK_INVALID_CHARACTER) },
                { ChooseSecretCharacterResultCode.SecretAlreadyChosen,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_SECRET_ALREADY_CHOSEN,
                        MatchLifecycleFaultKeys.MSG_SECRET_ALREADY_CHOSEN,
                        MatchLifecycleFaultKeys.FALLBACK_SECRET_ALREADY_CHOSEN) },
                { ChooseSecretCharacterResultCode.OperationConflict,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT,
                        MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT,
                        MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT) }
            };

        private static readonly IReadOnlyDictionary<ChangeSecretCharacterResultCode, FaultTriplet> FaultChangeMap =
            new Dictionary<ChangeSecretCharacterResultCode, FaultTriplet>
            {
                { ChangeSecretCharacterResultCode.MatchNotFound,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND,
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND,
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND) },
                { ChangeSecretCharacterResultCode.MatchNotInLobby,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_IN_LOBBY,
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_IN_LOBBY,
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_IN_LOBBY) },
                { ChangeSecretCharacterResultCode.PlayerNotInMatch,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_PLAYER_NOT_IN_MATCH,
                        MatchLifecycleFaultKeys.MSG_PLAYER_NOT_IN_MATCH,
                        MatchLifecycleFaultKeys.FALLBACK_PLAYER_NOT_IN_MATCH) },
                { ChangeSecretCharacterResultCode.PlayerAlreadyLeft,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_PLAYER_ALREADY_LEFT,
                        MatchLifecycleFaultKeys.MSG_PLAYER_ALREADY_LEFT,
                        MatchLifecycleFaultKeys.FALLBACK_PLAYER_ALREADY_LEFT) },
                { ChangeSecretCharacterResultCode.InvalidCharacter,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_INVALID_CHARACTER,
                        MatchLifecycleFaultKeys.MSG_INVALID_CHARACTER,
                        MatchLifecycleFaultKeys.FALLBACK_INVALID_CHARACTER) },
                { ChangeSecretCharacterResultCode.OperationConflict,
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT,
                        MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT,
                        MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT) }
            };

        public MatchSecretCharacterLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IMatchCallbackDispatcher callbackDispatcher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.callbackDispatcher = callbackDispatcher ?? 
                throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            string trimmedCharacterId = ValidateAndTrimInput(request, request.MatchId, request.UserId);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            var chooseArgs = new ChooseSecretCharacterArgs
            {
                MatchId = request.MatchId,
                UserProfileId = request.UserId,
                SecretCharacterId = trimmedCharacterId
            };

            ChooseSecretCharacterResult chooseResult = unitOfWork.Matches.ChooseSecretCharacter(chooseArgs);

            if (!chooseResult.IsSuccess)
            {
                string logMessage = string.Format("ChooseSecret failed. MatchId={0}, UserId={1}, Code={2}.",
                    request.MatchId, request.UserId, chooseResult.Code);

                ThrowMappedFault(
                    FaultChooseMap,
                    chooseResult.Code,
                    CONTEXT_CHOOSE_SECRET,
                    logMessage);
            }

            FinalizeSelection(unitOfWork, request.MatchId, request.UserId);

            return new BasicResponse { Success = true };
        }

        public BasicResponse ChangeSecretCharacter(ChangeSecretCharacterRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            string trimmedCharacterId = ValidateAndTrimInput(request, request.MatchId, request.ProfileId);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            var changeArgs = new ChangeSecretCharacterArgs(
                request.MatchId,
                request.ProfileId,
                trimmedCharacterId
            );

            ChangeSecretCharacterResult changeResult = unitOfWork.Matches.ChangeSecretCharacter(changeArgs);

            if (!changeResult.IsSuccess)
            {
                string logMessage = string.Format("ChangeSecret failed. MatchId={0}, ProfileId={1}, Code={2}.",
                    request.MatchId, request.ProfileId, changeResult.Code);

                ThrowMappedFault(
                    FaultChangeMap,
                    changeResult.Code,
                    CONTEXT_CHANGE_SECRET,
                    logMessage);
            }

            FinalizeSelection(unitOfWork, request.MatchId, request.ProfileId);

            return new BasicResponse { Success = true };
        }

        private static string ValidateAndTrimInput(object request, long matchId, long userId)
        {
            if (matchId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_MATCH_ID);
            }

            if (userId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_USER_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_USER_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_USER_ID);
            }

            string rawCharacterId;

            if (request is ChooseSecretCharacterRequest chooseRequest)
            {
                rawCharacterId = chooseRequest.CharacterId;
            }
            else if (request is ChangeSecretCharacterRequest changeRequest)
            {
                rawCharacterId = changeRequest.CharacterId;
            }
            else
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            string trimmedCharacterId = (rawCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedCharacterId))
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_CHARACTER,
                    MatchLifecycleFaultKeys.MSG_INVALID_CHARACTER,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_CHARACTER);
            }

            return trimmedCharacterId;
        }

        private void FinalizeSelection(IGuessWhoUnitOfWork unitOfWork, long matchId, long userId)
        {
            unitOfWork.Flush();

            callbackDispatcher.Broadcast(
                matchId,
                callback => callback.OnSecretCharacterChosen(matchId, userId));

            if (!unitOfWork.Matches.AreAllSecretCharactersChosen(matchId))
            {
                return;
            }

            TryInitializeFirstTurnIfReady(unitOfWork, matchId);

            callbackDispatcher.Broadcast(
                matchId,
                callback => callback.OnAllSecretCharactersChosen(matchId));
        }

        private void TryInitializeFirstTurnIfReady(IGuessWhoUnitOfWork unitOfWork, long matchId)
        {
            if (unitOfWork == null || matchId <= INVALID_ID)
            {
                return;
            }

            TurnStateSnapshot existingTurnState = unitOfWork.MatchesTurns.GetTurnState(matchId);

            if (existingTurnState != null && existingTurnState.MatchId > 0)
            {
                return;
            }

            IReadOnlyList<long> activePlayerUserIds = unitOfWork.Matches.GetActivePlayerIds(matchId, TURN_PLAYERS_REQUIRED);

            if (activePlayerUserIds.Count != TURN_PLAYERS_REQUIRED)
            {
                Logger.WarnFormat(
                    "{0}: skipping turn init. Expected {1} active players but got {2}. matchId={3}.",
                    CONTEXT_TURN_INIT,
                    TURN_PLAYERS_REQUIRED,
                    activePlayerUserIds.Count,
                    matchId);

                return;
            }

            long firstCandidateUserId = activePlayerUserIds[0];
            long secondCandidateUserId = activePlayerUserIds[1];

            if (firstCandidateUserId <= 0 || secondCandidateUserId <= 0 || 
                firstCandidateUserId == secondCandidateUserId)
            {
                Logger.WarnFormat(
                    "{0}: skipping turn init. Expected {1} active players but got {2}. matchId={3}.",
                    CONTEXT_TURN_INIT,
                    TURN_PLAYERS_REQUIRED,
                    activePlayerUserIds.Count,
                    matchId);

                return;
            }

            RandomBoolResult randomStartDecision = TryGetRandomBool();

            bool shouldFirstCandidateStart = !randomStartDecision.IsSuccess || randomStartDecision.Value;

            if (!randomStartDecision.IsSuccess)
            {
                Logger.WarnFormat("{0}: RNG unavailable. Using deterministic first player. matchId={1}.", 
                    CONTEXT_TURN_INIT, matchId);
            }

            long startingPlayerUserId =
                shouldFirstCandidateStart ? firstCandidateUserId : secondCandidateUserId;
            long nonStartingPlayerUserId =
                shouldFirstCandidateStart ? secondCandidateUserId : firstCandidateUserId;

            var initializeTurnOrderArgs = new InitializeTurnOrderArgs
            {
                MatchId = matchId,
                UserIdsInOrder = new List<long>(TURN_PLAYERS_REQUIRED) { startingPlayerUserId, nonStartingPlayerUserId },
                NowUtc = DateTime.UtcNow
            };

            InitializeTurnOrderResult initializeTurnOrderResult =
                unitOfWork.MatchesTurns.InitializeTurnOrder(initializeTurnOrderArgs);

            Logger.InfoFormat("{0}: turn init result. matchId={1} code={2}.", 
                CONTEXT_TURN_INIT, matchId, initializeTurnOrderResult.Code);

            if (initializeTurnOrderResult.IsSuccess)
            {
                unitOfWork.Flush();
            }
            else if (initializeTurnOrderResult.Code ==
                InitializeTurnOrderResultCode.AlreadyInitialized ||
                initializeTurnOrderResult.Code ==
                InitializeTurnOrderResultCode.OperationConflict)
            {
                _ = unitOfWork.MatchesTurns.GetTurnState(matchId);
            }
        }

        private static RandomBoolResult TryGetRandomBool()
        {
            try
            {
                byte[] randomByteBuffer = new byte[RANDOM_BYTE_COUNT];

                using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
                {
                    randomNumberGenerator.GetBytes(randomByteBuffer);
                }

                bool generatedValue = (randomByteBuffer[0] & RANDOM_BIT_MASK) == RANDOM_BIT_MASK;

                return RandomBoolResult.Success(generatedValue);
            }
            catch (CryptographicException ex)
            {
                Logger.Debug("RandomNumberGenerator failed.", ex);
                return RandomBoolResult.Fail();
            }
            catch (PlatformNotSupportedException ex)
            {
                Logger.Debug("RandomNumberGenerator failed.", ex);
                return RandomBoolResult.Fail();
            }
        }

        private static void ThrowMappedFault<TCode>(
            IReadOnlyDictionary<TCode, FaultTriplet> map,
            TCode code,
            string context,
            string logMessage)
        {
            if (map.TryGetValue(code, out FaultTriplet triplet))
            {
                Logger.InfoFormat("{0}: {1}", context, logMessage);
                throw FaultsFactory.Create(triplet.Code, triplet.MessageKey, triplet.Fallback);
            }

            Logger.ErrorFormat("{0}: unmapped result code. code={1}.", context, code);

            throw FaultsFactory.Create(
                InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
        }
    }
}
