using System;

namespace GuessWhoServerDomain.Domain.Parameters.Turns
{
    public sealed class AnswerQuestionArgs
    {
        public long MatchId { get; init; }
        public long UserId { get; init; }
        public int AnswerOptionId { get; init; }
        public DateTime NowUtc { get; init; }
    }
}
