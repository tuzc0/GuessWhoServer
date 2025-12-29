using System;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public class JoinMatchArgs
    {
        public long MatchId { get; set; }
        public long UserProfileId { get; set; }
        public string MatchCode { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}
