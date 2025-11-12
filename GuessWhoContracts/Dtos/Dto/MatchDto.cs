using System;

namespace GuessWhoContracts.Dtos.Dto
{
    public sealed class MatchDto
    {
        public const long INVALID_MATCH_ID = -1;
        public const byte INVALID_STATUS_ID = 0;
        public const byte INVALID_VISIBILITY_ID = 0;

        public long MatchId { get; set; }
        public string Code { get; set; }
        public byte StatusId { get; set; }
        public string Mode { get; set; }
        public byte VisibilityId { get; set; }
        public DateTime CreateAtUtc { get; set; }

        public bool IsValid
        {
            get { return MatchId != INVALID_MATCH_ID; }
        }

        public static MatchDto CreateInvalid(string code = null)
        {
            return new MatchDto
            {
                MatchId = INVALID_MATCH_ID,
                Code = code ?? string.Empty,
                StatusId = INVALID_STATUS_ID,
                Mode = null,
                VisibilityId = INVALID_VISIBILITY_ID,
                CreateAtUtc = DateTime.MinValue
            };
        }

    }
}
