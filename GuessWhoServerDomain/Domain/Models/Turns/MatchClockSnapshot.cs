namespace GuessWhoServerDomain.Domain.Models.Turns
{
    public readonly record struct MatchClockSnapshot(
        long MatchId,
        long UserId,
        int SecondsConsumed,
        int LimitSeconds)
    {
        private const long INVALID_ID = 0;

        public bool IsValid =>
            MatchId > INVALID_ID &&
            UserId > INVALID_ID &&
            SecondsConsumed >= 0 &&
            LimitSeconds > 0;

        public static MatchClockSnapshot Invalid()
        {
            return new MatchClockSnapshot(0, 0, 0, 0);
        }
    }
}
