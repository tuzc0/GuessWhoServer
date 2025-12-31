using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Faults.Match;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;

namespace GuessWhoServices.Services.MatchApplication
{
    public sealed class MatchSecretCharacterLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchSecretCharacterLogic));

        private const string CONTEXT_CHOOSE_SECRET = "MatchSecretCharacterLogic.ChooseSecretCharacter";
        private const string CONTEXT_CHANGE_SECRET = "MatchSecretCharacterLogic.ChangeSecretCharacter";

        private const long INVALID_ID = 0;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IMatchCallbackDispatcher callbackDispatcher;

        private readonly record struct FaultTriplet(string Code, string MessageKey, string Fallback);

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
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_SECRET_ALREADY_CHOSEN, 
                        MatchLifecycleFaultKeys.MSG_SECRET_ALREADY_CHOSEN, 
                        MatchLifecycleFaultKeys.FALLBACK_SECRET_ALREADY_CHOSEN) },
                { ChooseSecretCharacterResultCode.OperationConflict,  
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT) }
            };

        private static readonly IReadOnlyDictionary<ChangeSecretCharacterResultCode, FaultTriplet> FaultChangeMap =
            new Dictionary<ChangeSecretCharacterResultCode, FaultTriplet>
            {
                { ChangeSecretCharacterResultCode.MatchNotFound,      
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND) },
                { ChangeSecretCharacterResultCode.MatchNotInLobby,    
                    new FaultTriplet(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_IN_LOBBY, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_IN_LOBBY, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_IN_LOBBY) },
                { ChangeSecretCharacterResultCode.PlayerNotInMatch,   
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_PLAYER_NOT_IN_MATCH, 
                        MatchLifecycleFaultKeys.MSG_PLAYER_NOT_IN_MATCH, 
                        MatchLifecycleFaultKeys.FALLBACK_PLAYER_NOT_IN_MATCH) },
                { ChangeSecretCharacterResultCode.PlayerAlreadyLeft,  
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_PLAYER_ALREADY_LEFT, 
                        MatchLifecycleFaultKeys.MSG_PLAYER_ALREADY_LEFT, 
                        MatchLifecycleFaultKeys.FALLBACK_PLAYER_ALREADY_LEFT) },
                { ChangeSecretCharacterResultCode.InvalidCharacter,   
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_INVALID_CHARACTER, 
                        MatchLifecycleFaultKeys.MSG_INVALID_CHARACTER, 
                        MatchLifecycleFaultKeys.FALLBACK_INVALID_CHARACTER) },
                { ChangeSecretCharacterResultCode.OperationConflict,  
                    new FaultTriplet(MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT) }
            };

        public MatchSecretCharacterLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IMatchCallbackDispatcher callbackDispatcher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.callbackDispatcher = callbackDispatcher ?? throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            string characterId = ValidateAndTrimInput(request, request?.MatchId ?? 0, request?.UserId ?? 0);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            var args = new ChooseSecretCharacterArgs
            {
                MatchId = request.MatchId,
                UserProfileId = request.UserId,
                SecretCharacterId = characterId
            };

            ChooseSecretCharacterResult result = unitOfWork.Matches.ChooseSecretCharacter(args);

            if (!result.IsSuccess)
            {
                ThrowMappedFault(FaultChooseMap, result.Code, CONTEXT_CHOOSE_SECRET,
                    $"ChooseSecret failed. MatchId={request.MatchId}, UserId={request.UserId}, Code={result.Code}.");
            }

            FinalizeSelection(unitOfWork, request.MatchId, request.UserId);

            return new BasicResponse { Success = true };
        }

        public BasicResponse ChangeSecretCharacter(ChangeSecretCharacterRequest request)
        {
            string characterId = ValidateAndTrimInput(request, request?.MatchId ?? 0, request?.ProfileId ?? 0);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            var args = new ChangeSecretCharacterArgs(
                request.MatchId,
                request.ProfileId,
                characterId
            );

            ChangeSecretCharacterResult result = unitOfWork.Matches.ChangeSecretCharacter(args);

            if (!result.IsSuccess)
            {
                ThrowMappedFault(FaultChangeMap, result.Code, CONTEXT_CHANGE_SECRET,
                    $"ChangeSecret failed. MatchId={request.MatchId}, ProfileId={request.ProfileId}, Code={result.Code}.");
            }

            FinalizeSelection(unitOfWork, request.MatchId, request.ProfileId);

            return new BasicResponse { Success = true };
        }

        private static string ValidateAndTrimInput(object request, long matchId, long userId)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

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

            string rawCharacterId = null;

            if (request is ChooseSecretCharacterRequest c1)
            {
                rawCharacterId = c1.CharacterId;
            }
            else if (request is ChangeSecretCharacterRequest c2)
            {
                rawCharacterId = c2.CharacterId;
            }

            string trimmedId = (rawCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(trimmedId))
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_CHARACTER,
                    MatchLifecycleFaultKeys.MSG_INVALID_CHARACTER,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_CHARACTER);
            }

            return trimmedId;
        }

        private void FinalizeSelection(IGuessWhoUnitOfWork unitOfWork, long matchId, long userId)
        {
            unitOfWork.Flush();

            callbackDispatcher.Broadcast(matchId,
                callback => callback.OnSecretCharacterChosen(matchId, userId));

            if (unitOfWork.Matches.AreAllSecretCharactersChosen(matchId))
            {
                callbackDispatcher.Broadcast(matchId,
                    callback => callback.OnAllSecretCharactersChosen(matchId));
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

            Logger.ErrorFormat("{0}: unmapped result code. {1}", context, logMessage);

            throw FaultsFactory.Create(
                InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
        }
    }
}