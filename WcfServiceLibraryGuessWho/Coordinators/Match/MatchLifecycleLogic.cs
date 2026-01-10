using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServices.Infrastructure;
using GuessWhoServices.Security;
using GuessWhoServices.Coordinators.Tournament;
using log4net;
using System;

namespace GuessWhoServices.Coordinators.Match
{
    public sealed class MatchLifecycleLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchLifecycleLogic));

        private const string CONTEXT_CREATE = "MatchLifecycleLogic.CreateMatch";
        private const long INVALID_ID = 0;
        private const int MAX_CREATE_ATTEMPTS = 12;

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;
        private readonly IMatchCallbackDispatcher callbackDispatcher;
        private readonly TournamentLobbyLogic tournamentLobbyLogic;

        public MatchLifecycleLogic(
            IGuessWhoUnitOfWorkFactory unitOfWorkFactory,
            IMatchCallbackDispatcher callbackDispatcher,
            TournamentLobbyLogic tournamentLobbyLogic)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ??
                throw new ArgumentNullException(nameof(unitOfWorkFactory));
            this.callbackDispatcher = callbackDispatcher ??
                throw new ArgumentNullException(nameof(callbackDispatcher));
            this.tournamentLobbyLogic = tournamentLobbyLogic ??
                throw new ArgumentNullException(nameof(tournamentLobbyLogic));
        }

        public MatchSnapshot CreateMatch(long hostUserId, DateTime nowUtc)
        {
            if (hostUserId <= INVALID_ID)
            {
                return MatchSnapshot.CreateInvalid();
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            for (int attempt = 0; attempt < MAX_CREATE_ATTEMPTS; attempt++)
            {
                string matchCode = CodeGenerator.GenerateNumericCode();

                var args = new CreateMatchArgs
                {
                    UserProfileId = hostUserId,
                    MatchCode = matchCode,
                    Visibility = MatchVisibility.Private,
                    MatchStatus = MatchStatus.Lobby,
                    Mode = MatchMode.Classic,
                    CreateDate = nowUtc
                };

                MatchSnapshot snapshot = unitOfWork.Matches.CreateMatchClassic(args);

                if (snapshot.IsValid)
                {
                    return snapshot;
                }
            }

            Logger.WarnFormat("{0}: create match failed after retries. ProfileId={1}.",
                CONTEXT_CREATE, hostUserId);

            return MatchSnapshot.CreateInvalid();
        }

        public StartMatchResult StartMatch(long matchId, long userId)
        {
            if (matchId <= INVALID_ID || userId <= INVALID_ID)
            {
                return StartMatchResult.Fail(StartMatchResultCode.InvalidArgs);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            StartMatchResult result = unitOfWork.Matches.StartMatch(matchId, userId);

            if (result.IsSuccess)
            {
                unitOfWork.Flush();

                callbackDispatcher.Broadcast(matchId, callback => callback.OnGameStarted(matchId));

                return StartMatchResult.Success();
            }

            return StartMatchResult.Fail(result.Code);
        }

        public EndMatchResult EndMatch(long matchId)
        {
            if (matchId <= INVALID_ID)
            {
                return EndMatchResult.Fail(EndMatchResultCode.InvalidArgs);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            EndMatchResult result = unitOfWork.Matches.EndMatch(new EndMatchArgs
            {
                MatchId = matchId
            });

            if (!result.IsSuccess)
            {
                return result;
            }

            unitOfWork.Flush();

            callbackDispatcher.Broadcast(matchId, callback =>
                callback.OnGameEnded(matchId, result.WinnerUserId));

            tournamentLobbyLogic.HandleMatchFinished(matchId, result.WinnerUserId);

            return result;
        }

        public SetMatchPrivateResult SetMatchPrivate(long matchId, long callerUserId)
        {
            if (matchId <= INVALID_ID || callerUserId <= INVALID_ID)
            {
                return SetMatchPrivateResult.Fail(SetMatchPrivateResultCode.InvalidArgs);
            }

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            SetMatchPrivateResult result =
                unitOfWork.Matches.SetMatchPrivate(matchId, callerUserId);

            if (result.IsSuccess)
            {
                unitOfWork.Flush();

                return SetMatchPrivateResult.Success();
            }

            return SetMatchPrivateResult.Fail(result.Code);
        }

        public MatchSnapshot SearchPublicMatch(string matchCode)
        {
            string normalizedCode = (matchCode ?? string.Empty).Trim().ToUpperInvariant();

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            MatchSnapshot snapshot = unitOfWork.Matches.GetOpenMatchByCode(normalizedCode);

            if (snapshot.IsValid && snapshot.VisibilityId == (byte)MatchVisibility.Public)
            {
                return snapshot;
            }

            return MatchSnapshot.CreateInvalid();
        }
    }
}