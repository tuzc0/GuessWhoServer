using System;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public class CreateTournamentMatchArgs
    {
        public long Player1UserId { get; init; }
        public long Player2UserId { get; init; }
        public DateTime NowUtc { get; init; }
    }
}
