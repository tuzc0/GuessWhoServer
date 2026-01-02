namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class RegisterQuestionArgs
    {
        public long MatchId { get; init; }
        public long AskingUserId { get; init; }
        public long AttributeId { get; init; }
    }
}
