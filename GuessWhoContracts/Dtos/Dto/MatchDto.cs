using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class MatchDto
    {
        public long MatchId { get; set; }
        public string Code { get; set; }
        public byte StatusId { get; set; }
        public string Mode { get; set; }
        public byte VisibilityId { get; set; }
        public DateTime CreateAtUtc { get; set; }
    }
}
