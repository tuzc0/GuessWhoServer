using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuessWhoServerDomain.Domain.Enums.Tournaments
{
    public enum TournamentStatus : byte
    {
        Lobby = 1,
        Active = 2,
        Finished = 3,
        Cancelled = 4
    }
}
