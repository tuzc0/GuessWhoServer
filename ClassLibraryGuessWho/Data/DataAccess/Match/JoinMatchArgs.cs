using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public class JoinMatchArgs
    {
        public long MatchId { get; set; }
        public long UserProfileId { get; set; }
        public string MatchCode { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}
