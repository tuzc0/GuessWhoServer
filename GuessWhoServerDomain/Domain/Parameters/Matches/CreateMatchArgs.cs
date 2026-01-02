using GuessWhoServerDomain.Domain.Enums.Matches;
using System;

namespace GuessWhoServerDomain.Domain.Parameters.Matches
{
    public sealed class CreateMatchArgs
    {
        public long UserProfileId { get; set; }
        public MatchStatus MatchStatus { get; set; }
        public MatchVisibility Visibility { get; set; }
        public MatchMode Mode { get; set; }
        public DateTime CreateDate { get; set; }
        public string MatchCode { get; set; }
    }
}