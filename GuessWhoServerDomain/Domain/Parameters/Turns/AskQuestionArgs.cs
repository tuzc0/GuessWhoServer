using System;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class AskQuestionArgs
    {
        public long MatchId { get; init; }
        public long UserId { get; init; }
        public int AttributeId { get; init; }
    }
}
