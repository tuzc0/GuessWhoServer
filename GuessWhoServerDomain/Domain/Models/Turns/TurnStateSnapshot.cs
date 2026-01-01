using System;

namespace GuessWhoServerDomain.Domain.Models.Turns
{
    public sealed class TurnStateSnapshot
    {
        public long MatchId { get; }
        public int TurnNumber { get; }
        public byte CurrentPosition { get; }
        public long CurrentUserId { get; }
        public DateTime TurnStartedAtUtc { get; }

        public TurnStateSnapshot(long matchId, int turnNumber, byte currentPosition, long currentUserId, DateTime turnStartedAtUtc)
        {
            MatchId = matchId;
            TurnNumber = turnNumber;
            CurrentPosition = currentPosition;
            CurrentUserId = currentUserId;
            TurnStartedAtUtc = turnStartedAtUtc;
        }
    }
}
