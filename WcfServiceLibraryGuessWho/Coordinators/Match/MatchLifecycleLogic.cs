using ClassLibraryGuessWho.Data.Factories;
using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Faults.Match;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Services.ErrorHandling;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace WcfServiceLibraryGuessWho.Coordinators.Match
{
    public sealed class MatchLifecycleLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchLifecycleLogic));

        private const string CONTEXT_CREATE = "MatchLifecycleLogic.CreateMatch";
        private const string CONTEXT_START = "MatchLifecycleLogic.StartMatch";
        private const string CONTEXT_END = "MatchLifecycleLogic.EndMatch";
        private const string CONTEXT_SET_PRIVATE = "MatchLifecycleLogic.SetMatchPrivate";

        private const long INVALID_ID = 0;

        private const int MATCH_CODE_LENGTH = 6;
        private const int MAX_CREATE_ATTEMPTS = 12;
        private const string MATCH_CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IMatchCallbackDispatcher callbackDispatcher;

        private readonly record struct HostAuthorizationContext(
            IGuessWhoUnitOfWork UnitOfWork,
            long MatchId,
            long CallerUserId,
            string Context);

        public MatchLifecycleLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IMatchCallbackDispatcher callbackDispatcher)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? 
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.callbackDispatcher = callbackDispatcher ?? 
                throw new ArgumentNullException(nameof(callbackDispatcher));
        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            ValidateCreateOrThrow(request);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            DateTime nowUtc = DateTime.UtcNow;

            for (int attempt = 0; attempt < MAX_CREATE_ATTEMPTS; attempt++)
            {
                string matchCode = GenerateMatchCode();

                var args = new CreateMatchArgs
                {
                    UserProfileId = request.ProfileId,
                    MatchCode = matchCode,
                    Visibility = MatchVisibility.Private,
                    MatchStatus = MatchStatus.Lobby,
                    Mode = MatchMode.Classic,
                    CreateDate = nowUtc
                };

                MatchSnapshot snapshot = unitOfWork.Matches.CreateMatchClassic(args);

                if (snapshot.IsValid)
                {
                    unitOfWork.Flush();

                    return new CreateMatchResponse
                    {
                        MatchId = snapshot.MatchId,
                        Code = snapshot.MatchCode ?? string.Empty,
                        StatusId = snapshot.StatusId,
                        VisibilityId = snapshot.VisibilityId,
                        ModeId = snapshot.ModeId,
                        CreateAtUtc = snapshot.CreatedAtUtc,
                        HostProfileId = request.ProfileId
                    };
                }
            }

            Logger.WarnFormat("{0}: create match failed after retries. ProfileId={1}.", 
                CONTEXT_CREATE, request.ProfileId);

            throw FaultsFactory.Create(
                MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT,
                MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT,
                MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT);
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            ValidateStartOrThrow(request);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            EnsureCallerIsHostOrThrow(
                new HostAuthorizationContext(
                    unitOfWork, 
                    request.MatchId, 
                    request.UserId, 
                    CONTEXT_START
                )
            );

            StartMatchResult result = unitOfWork.Matches.StartMatch(request.MatchId, request.UserId);

            if (result.IsSuccess)
            {
                unitOfWork.Flush();
                callbackDispatcher.Broadcast(request.MatchId, callback => callback.OnGameStarted(request.MatchId));
                return new BasicResponse { Success = true };
            }

            ThrowStartMatchFaultOrUnexpected(request, result);
            return new BasicResponse { Success = false };
        }

        public BasicResponse EndMatch(EndMatchRequest request)
        {
            ValidateEndOrThrow(request);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            EnsureCallerIsHostOrThrow(
                new HostAuthorizationContext(
                    unitOfWork,
                    request.MatchId, 
                    request.HostUserId, 
                    CONTEXT_END
                )
            );

            var args = new EndMatchArgs
            {
                MatchId = request.MatchId,
                WinnerUserId = request.WinnerUserId
            };

            EndMatchResult result = unitOfWork.Matches.EndMatch(args);

            if (result.IsSuccess)
            {
                unitOfWork.Flush();
                callbackDispatcher.Broadcast(request.MatchId, callback => 
                callback.OnGameEnded(request.MatchId, request.WinnerUserId));

                return new BasicResponse { Success = true };
            }

            ThrowEndMatchFaultOrUnexpected(request, result);
            return new BasicResponse { Success = false };
        }

        public BasicResponse SetMatchPrivate(SetMatchPrivateRequest request)
        {
            ValidateSetPrivateOrThrow(request);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            EnsureCallerIsHostOrThrow(
                new HostAuthorizationContext(
                    unitOfWork, 
                    request.MatchId, 
                    request.ProfileId, 
                    CONTEXT_SET_PRIVATE
                )
            );

            SetMatchPrivateResult result = 
                unitOfWork.Matches.SetMatchPrivate(request.MatchId, request.ProfileId);

            if (result.IsSuccess)
            {
                unitOfWork.Flush();
                return new BasicResponse { Success = true };
            }

            ThrowSetPrivateFaultOrUnexpected(request, result);
            return new BasicResponse { Success = false };
        }

        public SearchPublicMatchResponse SearchPublicMatch(SearchPublicMatchRequest request)
        {
            ValidateSearchPublicOrThrow(request);

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            string normalizedMatchCode = NormalizeMatchCode(request.MatchCode);

            MatchSnapshot snapshot = unitOfWork.Matches.GetOpenMatchByCode(normalizedMatchCode);

            if (!snapshot.IsValid)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND,
                    MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND,
                    MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND);
            }

            if (snapshot.VisibilityId != (byte)MatchVisibility.Public)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_MATCH_NOT_PUBLIC,
                    MatchLifecycleFaultKeys.MSG_MATCH_NOT_PUBLIC,
                    MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_PUBLIC);
            }

            return new SearchPublicMatchResponse
            {
                Match = new PublicLobbyMatchDto
                {
                    MatchId = snapshot.MatchId,
                    MatchCode = snapshot.MatchCode ?? string.Empty,
                    StatusId = snapshot.StatusId,
                    VisibilityId = snapshot.VisibilityId,
                    ModeId = snapshot.ModeId,
                    CreatedAtUtc = snapshot.CreatedAtUtc
                }
            };
        }

        private static void ValidateSearchPublicOrThrow(SearchPublicMatchRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            string matchCode = (request.MatchCode ?? string.Empty).Trim();

            if (matchCode.Length != MATCH_CODE_LENGTH)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_MATCH_CODE,
                    MatchLifecycleFaultKeys.MSG_INVALID_MATCH_CODE,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_MATCH_CODE);
            }
        }

        private static string NormalizeMatchCode(string matchCode)
        {
            return (matchCode ?? string.Empty).Trim().ToUpperInvariant();
        }


        private static void ValidateCreateOrThrow(CreateMatchRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            if (request.ProfileId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_PROFILE_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_PROFILE_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_PROFILE_ID);
            }
        }

        private static void ValidateStartOrThrow(StartMatchRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            if (request.MatchId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_MATCH_ID);
            }

            if (request.UserId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_USER_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_USER_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_USER_ID);
            }
        }

        private static void ValidateEndOrThrow(EndMatchRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            if (request.MatchId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_MATCH_ID);
            }

            if (request.HostUserId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_USER_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_USER_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_USER_ID);
            }

            if (request.WinnerUserId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_WINNER,
                    MatchLifecycleFaultKeys.MSG_INVALID_WINNER,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_WINNER);
            }
        }

        private static void ValidateSetPrivateOrThrow(SetMatchPrivateRequest request)
        {
            if (request == null)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);
            }

            if (request.MatchId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_MATCH_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_MATCH_ID);
            }

            if (request.ProfileId <= INVALID_ID)
            {
                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_INVALID_PROFILE_ID,
                    MatchLifecycleFaultKeys.MSG_INVALID_PROFILE_ID,
                    MatchLifecycleFaultKeys.FALLBACK_INVALID_PROFILE_ID);
            }
        }

        private static void EnsureCallerIsHostOrThrow(HostAuthorizationContext authContext)
        {
            long hostUserId = ResolveHostUserId(authContext.UnitOfWork, authContext.MatchId);

            if (hostUserId <= INVALID_ID)
            {
                Logger.ErrorFormat("{0}: host could not be resolved. MatchId={1}, CallerUserId={2}.",
                    authContext.Context, authContext.MatchId, authContext.CallerUserId);

                throw FaultsFactory.Create(
                    InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR,
                    InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR,
                    InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
            }

            if (hostUserId != authContext.CallerUserId)
            {
                Logger.WarnFormat("{0}: caller is not host. MatchId={1}, CallerUserId={2}, HostUserId={3}.",
                    authContext.Context, authContext.MatchId, authContext.CallerUserId, hostUserId);

                throw FaultsFactory.Create(
                    MatchLifecycleFaultKeys.CODE_HOST_NOT_AUTHORIZED,
                    MatchLifecycleFaultKeys.MSG_HOST_NOT_AUTHORIZED,
                    MatchLifecycleFaultKeys.FALLBACK_HOST_NOT_AUTHORIZED);
            }
        }

        private static long ResolveHostUserId(IGuessWhoUnitOfWork unitOfWork, long matchId)
        {
            IReadOnlyList<LobbyPlayerSnapshot> players =
                unitOfWork.Matches.GetMatchPlayers(matchId) ?? Array.Empty<LobbyPlayerSnapshot>();

            foreach (LobbyPlayerSnapshot player in players)
            {
                if (player.IsHost && player.UserId > INVALID_ID)
                {
                    return player.UserId;
                }
            }

            return INVALID_ID;
        }

        private static void ThrowStartMatchFaultOrUnexpected(StartMatchRequest request, StartMatchResult result)
        {
            Logger.InfoFormat("{0}: StartMatch failed. MatchId={1}, HostUserId={2}, Code={3}.",
                CONTEXT_START, request.MatchId, request.UserId, result.Code);

            switch (result.Code)
            {
                case StartMatchResultCode.MatchNotFound:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND);

                case StartMatchResultCode.MatchNotInLobby:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_IN_LOBBY, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_IN_LOBBY, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_IN_LOBBY);

                case StartMatchResultCode.NotEnoughPlayers:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_NOT_ENOUGH_PLAYERS, 
                        MatchLifecycleFaultKeys.MSG_NOT_ENOUGH_PLAYERS, 
                        MatchLifecycleFaultKeys.FALLBACK_NOT_ENOUGH_PLAYERS);

                case StartMatchResultCode.PlayersNotReady:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_PLAYERS_NOT_READY, 
                        MatchLifecycleFaultKeys.MSG_PLAYERS_NOT_READY, 
                        MatchLifecycleFaultKeys.FALLBACK_PLAYERS_NOT_READY);

                case StartMatchResultCode.ConcurrentUpdate:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT);

                default:
                    Logger.ErrorFormat("{0}: unmapped StartMatchResultCode. MatchId={1}, HostUserId={2}, Code={3}.",
                        CONTEXT_START, request.MatchId, request.UserId, result.Code);

                    throw FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR, 
                        InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR, 
                        InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
            }
        }

        private static void ThrowEndMatchFaultOrUnexpected(EndMatchRequest request, EndMatchResult result)
        {
            Logger.InfoFormat("{0}: EndMatch failed. MatchId={1}, HostUserId={2}, WinnerUserId={3}, Code={4}.",
                CONTEXT_END, request.MatchId, request.HostUserId, request.WinnerUserId, result.Code);

            switch (result.Code)
            {
                case EndMatchResultCode.MatchNotFound:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND);

                case EndMatchResultCode.MatchNotInProgress:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_IN_PROGRESS, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_IN_PROGRESS, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_IN_PROGRESS);

                case EndMatchResultCode.WinnerNotInMatch:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_WINNER_NOT_IN_MATCH,
                        MatchLifecycleFaultKeys.MSG_WINNER_NOT_IN_MATCH, 
                        MatchLifecycleFaultKeys.FALLBACK_WINNER_NOT_IN_MATCH);

                default:
                    Logger.ErrorFormat("{0}: unmapped EndMatchResultCode. MatchId={1}, HostUserId={2}, WinnerUserId={3}, Code={4}.",
                        CONTEXT_END, request.MatchId, request.HostUserId, request.WinnerUserId, result.Code);

                    throw FaultsFactory.Create(
                        InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR, 
                        InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR, 
                        InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
            }
        }

        private static void ThrowSetPrivateFaultOrUnexpected(SetMatchPrivateRequest request, SetMatchPrivateResult result)
        {
            Logger.InfoFormat("{0}: SetMatchPrivate failed. MatchId={1}, HostUserId={2}, Code={3}.",
                CONTEXT_SET_PRIVATE, request.MatchId, request.ProfileId, result.Code);

            switch (result.Code)
            {
                case SetMatchPrivateResultCode.MatchNotFound:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_FOUND, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_FOUND);

                case SetMatchPrivateResultCode.MatchNotInLobby:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_NOT_IN_LOBBY, 
                        MatchLifecycleFaultKeys.MSG_MATCH_NOT_IN_LOBBY, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_NOT_IN_LOBBY);

                case SetMatchPrivateResultCode.HostNotAuthorized:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_HOST_NOT_AUTHORIZED, 
                        MatchLifecycleFaultKeys.MSG_HOST_NOT_AUTHORIZED, 
                        MatchLifecycleFaultKeys.FALLBACK_HOST_NOT_AUTHORIZED);

                case SetMatchPrivateResultCode.AlreadyPrivate:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_MATCH_ALREADY_PRIVATE, 
                        MatchLifecycleFaultKeys.MSG_MATCH_ALREADY_PRIVATE, 
                        MatchLifecycleFaultKeys.FALLBACK_MATCH_ALREADY_PRIVATE);

                case SetMatchPrivateResultCode.ConcurrentUpdate:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.MSG_OPERATION_CONFLICT, 
                        MatchLifecycleFaultKeys.FALLBACK_OPERATION_CONFLICT);

                case SetMatchPrivateResultCode.InvalidArgs:
                    throw FaultsFactory.Create(
                        MatchLifecycleFaultKeys.CODE_INVALID_REQUEST, 
                        MatchLifecycleFaultKeys.MSG_INVALID_REQUEST,
                        MatchLifecycleFaultKeys.FALLBACK_INVALID_REQUEST);

                default:
                    Logger.ErrorFormat("{0}: unmapped SetMatchPrivateResultCode. MatchId={1}, HostUserId={2}, Code={3}.",
                        CONTEXT_SET_PRIVATE, request.MatchId, request.ProfileId, result.Code);

                    throw FaultsFactory.Create(InfrastructureFaultKeys.CODE_UNEXPECTED_ERROR, InfrastructureFaultKeys.MSG_UNEXPECTED_ERROR, InfrastructureFaultKeys.FALLBACK_UNEXPECTED_ERROR);
            }
        }

        private static string GenerateMatchCode()
        {
            byte[] randomBytes = new byte[MATCH_CODE_LENGTH];
            char[] chars = new char[MATCH_CODE_LENGTH];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            for (int index = 0; index < MATCH_CODE_LENGTH; index++)
            {
                int alphabetIndex = randomBytes[index] % MATCH_CODE_ALPHABET.Length;
                chars[index] = MATCH_CODE_ALPHABET[alphabetIndex];
            }

            return new string(chars);
        }
    }
}
