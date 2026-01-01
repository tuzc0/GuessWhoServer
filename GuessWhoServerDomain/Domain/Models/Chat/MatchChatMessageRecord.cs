using System;

namespace GuessWhoServerDomain.Domain.Models.Chat
{
    public sealed record MatchChatMessageRecord
    {
        public long MessageId { get; }
        public long MatchId { get; }
        public long SenderUserId { get; }
        public string Message { get; }
        public DateTime CreateAtUtc { get; }

        public MatchChatMessageRecord(long messageId, long matchId, long senderUserId, string message, DateTime createAtUtc)
        {
            MessageId = messageId;
            MatchId = matchId;
            SenderUserId = senderUserId;
            Message = message ?? string.Empty;
            CreateAtUtc = createAtUtc;
        }
    }
}
