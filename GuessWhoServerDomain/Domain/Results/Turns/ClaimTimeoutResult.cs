using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct ClaimTimeoutResult(
        ClaimTimeoutResultCode Code,
        long MatchId,
        long WinnerUserId,
        long TimedOutUserId,
        int TotalSeconds,
        int LimitSeconds)
    {
        public bool IsSuccess => Code == ClaimTimeoutResultCode.Success;

        public static ClaimTimeoutResult Success(
            long matchId,
            long winnerUserId,
            long timedOutUserId,
            int totalSeconds,
            int limitSeconds)
        {
            return new ClaimTimeoutResult(
                ClaimTimeoutResultCode.Success,
                matchId,
                winnerUserId,
                timedOutUserId,
                totalSeconds,
                limitSeconds);
        }

        public static ClaimTimeoutResult Fail(ClaimTimeoutResultCode code)
        {
            return new ClaimTimeoutResult(code, 0, 0, 0, 0, 0);
        }

        public static ClaimTimeoutResult NotTimedOut(
            long matchId,
            long winnerUserId,
            long timedOutUserId,
            int totalSeconds,
            int limitSeconds)
        {
            return new ClaimTimeoutResult(
                ClaimTimeoutResultCode.NotTimedOut,
                matchId,
                WinnerUserId: 0,
                TimedOutUserId: timedOutUserId,
                TotalSeconds: totalSeconds,
                LimitSeconds: limitSeconds);
        }
    }
}
