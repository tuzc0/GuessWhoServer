using System;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public class CreateTournamentMatchArgs
    {
        public long HostUserId { get; set; }
        public long CharacterSetId { get; set; }
        public int TurnSecond { get; set; }
        public byte StatusId { get; set; }
        public DateTime CreateAtUtc { get; set; }
    }
}
