using GuessWhoServerDomain.Domain.Enums.Turns;

namespace GuessWhoServerDomain.Domain.Results.Turns
{
    public readonly record struct ApplyChessClockResult(
        ApplyChessClockResultCode Code, 
        int SecondsConsumed, 
        int LimitSeconds, 
        long OpponentUserId
    )
    {
        private const int INVALID_SECONDS_CONSUMED = 0;
        private const int LIMIT_SECONDS_CONSUMED = 0;
        private const long INVALID_OPONNENT_ID = 0;

        public bool IsSuccess => Code == ApplyChessClockResultCode.Success;

        public bool IsTimeOut => IsSuccess && SecondsConsumed > LimitSeconds;

        public static ApplyChessClockResult Success(int secondConsumed, int limitSeconds, long opponentUserId) => 
            new(ApplyChessClockResultCode.Success, secondConsumed, limitSeconds, opponentUserId);

        public static ApplyChessClockResult Fail(ApplyChessClockResultCode code) => 
            new(code, INVALID_SECONDS_CONSUMED, LIMIT_SECONDS_CONSUMED, INVALID_OPONNENT_ID);
    }
}
    
