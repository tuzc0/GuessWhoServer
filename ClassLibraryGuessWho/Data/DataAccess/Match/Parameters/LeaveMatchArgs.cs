using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Match.Parameters
{
    public class LeaveMatchArgs
    {
        public long UserProfileId { get; set; }
        public long MatchId { get; set; }
        public DateTime LeftDate { get; set; }
    }
}
