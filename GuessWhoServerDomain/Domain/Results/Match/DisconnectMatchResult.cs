using GuessWhoServerDomain.Domain.Enums.Matches;

namespace GuessWhoServerDomain.Domain.Results.Match
{
    public readonly record struct DisconnectMatchResult(
        DisconnectMatchResultCode Code,
        long MatchId,
        long WinnerUserId)
    {
        private const long INVALID_MATCH_ID = 0;
        private const long INVALID_WINNER_ID = 0;

        public bool IsSuccess =>
            Code == DisconnectMatchResultCode.SuccessEndedWithWinner ||
            Code == DisconnectMatchResultCode.SuccessCancelledLobby ||
            Code == DisconnectMatchResultCode.SuccessLeftLobby;

        public static DisconnectMatchResult Fail(DisconnectMatchResultCode code)
        {
            return new DisconnectMatchResult(code, INVALID_MATCH_ID, INVALID_WINNER_ID);
        }
    }
}
