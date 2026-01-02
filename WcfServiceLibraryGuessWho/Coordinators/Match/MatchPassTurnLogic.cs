using GuessWhoDataAccess.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using log4net;
using System;

namespace GuessWhoServices.Coordinators.Match
{
    public sealed class MatchPassTurnLogic
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchPassTurnLogic));

        private const long INVALID_ID = 0;

        private const string LOG_TIMEOUT_FORMAT =
            "TimeOut. MatchId={0}, LoserUserId={1}, WinnerUserId={2}, Consumed={3}, Limit={4}.";

        private readonly IGuessWhoUnitOfWorkFactory unitOfWorkFactory;

        public MatchPassTurnLogic(IGuessWhoUnitOfWorkFactory unitOfWorkFactory)
        {
            this.unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
        }

        public PassTurnOutcome PassTurn(PassTurnArgs turnArgs)
        {
            if (turnArgs == null || turnArgs.MatchId <= INVALID_ID || turnArgs.UserId <= INVALID_ID)
            {
                return PassTurnOutcome.Fail(PassTurnOutcomeCode.InvalidArgs);
            }

            DateTime nowUtc = turnArgs.NowUtc == default ? DateTime.UtcNow : turnArgs.NowUtc;

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();
            using var transaction = unitOfWork.BeginTransaction();

            ApplyChessClockResult clock = unitOfWork.MatchChessClocks.ApplyOnPassTurn(new ApplyChessClockArgs
            {
                MatchId = turnArgs.MatchId,
                UserId = turnArgs.UserId,
                NowUtc = nowUtc
            });

            if (!clock.IsSuccess)
            {
                transaction.Rollback();
                return PassTurnOutcome.Fail(PassTurnOutcomeCode.OperationConflict);
            }

            if (clock.IsTimeOut)
            {
                Logger.WarnFormat(
                    LOG_TIMEOUT_FORMAT,
                    turnArgs.MatchId,
                    turnArgs.UserId,
                    clock.OpponentUserId,
                    clock.SecondsConsumed,
                    clock.LimitSeconds);

                DisconnectMatchResult disconnect = unitOfWork.Matches.HandleDisconnect(turnArgs.UserId, nowUtc);

                if (!disconnect.IsSuccess)
                {
                    transaction.Rollback();
                    return PassTurnOutcome.Fail(PassTurnOutcomeCode.OperationConflict);
                }

                unitOfWork.Flush();
                transaction.Commit();

                long winnerUserId = disconnect.WinnerUserId > INVALID_ID
                    ? disconnect.WinnerUserId
                    : clock.OpponentUserId;

                return PassTurnOutcome.TimeOut(clock.SecondsConsumed, clock.LimitSeconds, winnerUserId);
            }

            AdvanceTurnResult advance = unitOfWork.MatchTurnAdvances.AdvanceTurn(new AdvanceTurnArgs
            {
                MatchId = turnArgs.MatchId,
                UserId = turnArgs.UserId,
                ExpectedRowVersion = turnArgs.ExceptedRowVersion,
                NowUtc = nowUtc
            });

            if (!advance.IsSuccess)
            {
                transaction.Rollback();
                return PassTurnOutcome.Fail(PassTurnOutcomeCode.OperationConflict);
            }

            unitOfWork.Flush();
            transaction.Commit();

            return PassTurnOutcome.Success(advance.TurnState, clock.SecondsConsumed, clock.LimitSeconds);
        }

        private const int CLOCK_GRACE_SECONDS = 2;

        public ClaimTimeoutResult ClaimTimeout(ClaimTimeoutArgs claimTimeoutArgs)
        {
            if (!IsClaimTimeoutArgsValid(claimTimeoutArgs))
            {
                return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.InvalidArgs);
            }

            long matchId = claimTimeoutArgs.MatchId;
            long claimantUserId = claimTimeoutArgs.UserId;

            DateTime nowUtc = claimTimeoutArgs.NowUtc == default
                ? DateTime.UtcNow
                : claimTimeoutArgs.NowUtc;

            using IGuessWhoUnitOfWork unitOfWork = unitOfWorkFactory.Create();

            IMatchRepository matchRepository = unitOfWork.Matches;
            IMatchTurnRepository turnRepository = unitOfWork.MatchesTurns;
            IMatchChessClockRepository chessClockRepository = unitOfWork.MatchChessClocks;

            MatchSnapshot matchSnapshot = matchRepository.GetMatchById(matchId);

            if (!matchSnapshot.IsValid || matchSnapshot.MatchId <= INVALID_ID)
            {
                return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.MatchNotFound);
            }

            if (!IsMatchActive(matchSnapshot.StatusId))
            {
                return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.MatchNotInProgress);
            }

            TurnStateSnapshot turnStateSnapshot = turnRepository.GetTurnState(matchId);

            if (!IsTurnStateValid(turnStateSnapshot))
            {
                return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.TurnStateNotInitialized);
            }

            long activeUserId = turnStateSnapshot.CurrentUserId;

            if (activeUserId == claimantUserId)
            {
                return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.CannotClaimOnYourTurn);
            }

            MatchClockSnapshot activeClockSnapshot = chessClockRepository.GetPlayerClockState(matchId, activeUserId);

            if (!activeClockSnapshot.IsValid)
            {
                return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.ClockStateNotAvailable);
            }

            int elapsedSecondsThisTurn = CalculateElapsedSeconds(turnStateSnapshot.TurnStartedAtUtc, nowUtc);
            int totalSeconds = SafeAddSeconds(activeClockSnapshot.SecondsConsumed, elapsedSecondsThisTurn);
            int limitWithGraceSeconds = SafeAddSeconds(activeClockSnapshot.LimitSeconds, CLOCK_GRACE_SECONDS);

            if (totalSeconds <= limitWithGraceSeconds)
            {
                return ClaimTimeoutResult.NotTimedOut(
                    matchId,
                    winnerUserId: 0,
                    timedOutUserId: activeUserId,
                    totalSeconds: totalSeconds,
                    limitSeconds: activeClockSnapshot.LimitSeconds);

            }

            using (IGuessWhoDbTransaction transaction = unitOfWork.BeginTransaction())
            {
                var endMatchArgs = new EndMatchArgs
                {
                    MatchId = matchId,
                    WinnerUserId = claimantUserId
                };

                EndMatchResult endMatchResult = matchRepository.EndMatch(endMatchArgs);

                if (!endMatchResult.IsSuccess)
                {
                    transaction.Rollback();
                    return MapEndMatchFailure(endMatchResult);
                }

                unitOfWork.Flush();
                transaction.Commit();
            }

            return ClaimTimeoutResult.Success(
                matchId,
                winnerUserId: claimantUserId,
                timedOutUserId: activeUserId,
                totalSeconds: totalSeconds,
                limitSeconds: activeClockSnapshot.LimitSeconds);
        }

        private static bool IsClaimTimeoutArgsValid(ClaimTimeoutArgs claimTimeoutArgs)
        {
            return claimTimeoutArgs != null &&
                   claimTimeoutArgs.MatchId > INVALID_ID &&
                   claimTimeoutArgs.UserId > INVALID_ID;
        }

        private static bool IsMatchActive(byte statusId)
        {
            return statusId == (byte)MatchStatus.Active;
        }

        private static bool IsTurnStateValid(TurnStateSnapshot turnStateSnapshot)
        {
            return turnStateSnapshot.MatchId > INVALID_ID &&
                   turnStateSnapshot.CurrentUserId > INVALID_ID &&
                   turnStateSnapshot.TurnStartedAtUtc > DateTime.MinValue;
        }

        private static int CalculateElapsedSeconds(DateTime turnStartedAtUtc, DateTime nowUtc)
        {
            TimeSpan elapsed = nowUtc - turnStartedAtUtc;

            if (elapsed.Ticks <= 0)
            {
                return 0;
            }

            double seconds = elapsed.TotalSeconds;

            if (seconds <= 0)
            {
                return 0;
            }

            return (int)Math.Floor(seconds);
        }

        private static int SafeAddSeconds(int leftSeconds, int rightSeconds)
        {
            if (leftSeconds < 0)
            {
                leftSeconds = 0;
            }

            if (rightSeconds < 0)
            {
                rightSeconds = 0;
            }

            long sum = (long)leftSeconds + rightSeconds;

            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)sum;
        }

        private static ClaimTimeoutResult MapEndMatchFailure(EndMatchResult endMatchResult)
        {
            switch (endMatchResult.Code)
            {
                case EndMatchResultCode.MatchNotFound:
                    return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.MatchNotFound);

                case EndMatchResultCode.MatchNotInProgress:
                    return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.MatchNotInProgress);

                default:
                    return ClaimTimeoutResult.Fail(ClaimTimeoutResultCode.OperationConflict);
            }
        }
    }
}
