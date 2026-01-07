using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessWhoCore.Contracts.Requests
{
    public class GetLeaderboardRequest
    {
        public int TopN { get; set; }
        public long RequestingUserId { get; set; }
    }
}
