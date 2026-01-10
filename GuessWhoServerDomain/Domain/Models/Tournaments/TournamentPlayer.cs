using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoServerDomain.Domain.Models.Tournaments
{
    public class TournamentPlayer
    {
        public long TournamentId { get; set; }
        public long UserId { get; set; }
        public DateTime JoinedAtUtc { get; set; }
        public string DisplayName { get; set; } 
    }
}
