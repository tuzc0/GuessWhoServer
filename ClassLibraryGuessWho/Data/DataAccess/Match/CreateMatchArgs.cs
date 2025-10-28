using System;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public class CreateMatchArgs
    {
        public long UserProfileId { get; set; }
        public byte Visibility { get; set; }
        public string Mode { get; set; }
        public DateTime CreateDate { get; set; }
        public string MatchCode { get; set; }
    }
}