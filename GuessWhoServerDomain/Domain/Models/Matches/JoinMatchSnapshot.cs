using GuessWhoServerDomain.Domain.Models.Match;
using System;

namespace GuessWhoServerDomain.Domain.Models.Matches
{
    public sealed record JoinMatchSnapshot(
        long MatchId,
        string MatchCode,
        byte StatusId,
        byte ModeId,
        byte VisibilityId,
        DateTime CreatedAtUtc,
        long HostUserId,
        LobbyPlayerSnapshot[] Players)
    {
        public static JoinMatchSnapshot Empty()
        {
            return new JoinMatchSnapshot(
                MatchId: 0,
                MatchCode: string.Empty,
                StatusId: 0,
                ModeId: 0,
                VisibilityId: 0,
                CreatedAtUtc: DateTime.MinValue,
                HostUserId: 0,
                Players: Array.Empty<LobbyPlayerSnapshot>());
        }
    }
}
