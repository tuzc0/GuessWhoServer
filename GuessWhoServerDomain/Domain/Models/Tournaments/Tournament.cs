using GuessWhoServerDomain.Domain.Enums.Tournaments;
using System;

namespace GuessWhoServerDomain.Domain.Models.Tournaments
{
    public class Tournament
    {
        public long TournamentId { get; set; }
        public long HostUserId { get; set; }
        public int TurnSeconds { get; set; }
        public TournamentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}