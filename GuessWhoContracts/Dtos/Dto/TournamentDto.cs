using System;
using System.Collections.Generic;

namespace GuessWhoContracts.Dtos.Dto
{
    public class TournamentDto
    {
        public int TournamentId { get; set; }
        public long HostUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public byte StatusId { get; set; }
        public List<TournamentPlayerDto> Players { get; set; }
    }

}
