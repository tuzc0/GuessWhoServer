using ClassLibraryGuessWho.Data.Factories;
using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Match;
using GuessWhoServerDomain.Domain.Results.Turns;
using log4net;
using System;

namespace GuessWhoServices.Services.MatchApplication
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
    }
}
