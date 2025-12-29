using System;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class CreateMatchArgs
    {
        public long UserProfileId { get; set; }
        public byte MatchStatus { get; set; }
        public byte Visibility { get; set; }
        public byte Mode { get; set; }
        public DateTime CreateDate { get; set; }
        public string MatchCode { get; set; }
    }
}