namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class RegisterAnswerArgs
    {
        public long MatchId { get; init; }
        public long AnsweringUserId { get; init; }
        public int AnswerOptionId { get; init; }
    }
}
