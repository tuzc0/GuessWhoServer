using GuessWhoServerDomain.Domain.Models.Chat;
using GuessWhoServerDomain.Domain.Parameters.Chat;
using GuessWhoServerDomain.Domain.Results.Chat;
using System.Collections.Generic;

namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchChatRepository
    {
        AddMatchChatMessageResult AddMessage(AddMatchChatMessageArgs chatMessageArgs);

        IReadOnlyList<MatchChatMessageRecord> GetMessages(long matchId, int takeLast);
    }
}
