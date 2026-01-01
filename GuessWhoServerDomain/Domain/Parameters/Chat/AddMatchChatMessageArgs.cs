using System;

namespace GuessWhoServerDomain.Domain.Parameters.Chat
{
    public sealed class AddMatchChatMessageArgs
    {
        public long MatchId { get; set; }
        public long SenderUserId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAtUtc {get; set;}
    }
}
