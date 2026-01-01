using GuessWhoServerDomain.Domain.Enums.Turns;
using GuessWhoServerDomain.Domain.Models.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct PassTurnOutcome(
        PassTurnOutcomeCode Code, 
        TurnStateSnapshot TurnState,
        int SecondsConsumed, 
        int LimitSeconds, 
        long WinnerUserId)
    {
        private const int INVALID_WINNER_USER_ID = 0;
        private const int INVALID_SECONDS_CONSUMED = 0;
        private const int INVALID_LIMIT_SECONDS = 0;

        public bool IsSuccess => Code == PassTurnOutcomeCode.Success;

        public bool IsTimeOut => Code == PassTurnOutcomeCode.TimeOut;

        public static PassTurnOutcome Success(TurnStateSnapshot snapshot, int seconds, int limitSeconds) =>
            new(PassTurnOutcomeCode.Success, snapshot, seconds, limitSeconds, INVALID_WINNER_USER_ID);

        public static PassTurnOutcome TimeOut(int secondConsumed, int limitSeconds, long winnerUserId) =>
            new(PassTurnOutcomeCode.TimeOut, default, secondConsumed, limitSeconds, winnerUserId);

        public static PassTurnOutcome Fail(PassTurnOutcomeCode Code) =>
            new PassTurnOutcome(Code, default, INVALID_SECONDS_CONSUMED, INVALID_LIMIT_SECONDS, INVALID_WINNER_USER_ID);
    }
}
