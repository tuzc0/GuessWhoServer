using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData
    {
        private const byte HOST_SLOT_NUMBER = 1;
        private const byte GUEST_SLOT_NUMBER = 2;

        private const int MAX_MATCH_PLAYERS_BY_SCHEMA = 2;
        private const int MIN_PLAYERS_TO_START = 2;

        private static class MatchStatusIds
        {
            public const byte LOBBY = 1;
            public const byte ACTIVE = 2;
            public const byte FINISHED = 3;
            public const byte CANCELLED = 4;
        }

        private static class MatchVisibilityIds
        {
            public const byte PUBLIC = 1;
            public const byte PRIVATE = 2;
        }

        private static class MatchModeIds
        {
            public const byte CLASSIC = 1;
            public const byte QUICK = 2;
            public const byte TOURNAMENT_4P = 3;
        }

        private static bool IsLobbyMatch(MATCH match) =>
            match != null && match.STATUSID == MatchStatusIds.LOBBY;

        private static bool IsActiveMatch(MATCH match) =>
            match != null && match.STATUSID == MatchStatusIds.ACTIVE;

        private static bool IsMatchCompleted(MATCH match) =>
            match != null && (match.STATUSID == MatchStatusIds.FINISHED || match.STATUSID == MatchStatusIds.CANCELLED);

        private static bool IsActivePlayer(MATCH_PLAYER player) =>
            player != null && player.LEFTATUTC == null;

        private static IQueryable<MATCH_PLAYER> GetActivePlayersForMatch(GuessWhoDBEntities db, long matchId) =>
            db.MATCH_PLAYER.Where(mp => mp.MATCHID == matchId && mp.LEFTATUTC == null);

        private static IQueryable<MATCH_PLAYER> GetPlayersForMatch(GuessWhoDBEntities db, long matchId) =>
            db.MATCH_PLAYER.Where(mp => mp.MATCHID == matchId);

        private static void MarkPlayerAsLeft(MATCH_PLAYER player, DateTime utcNow)
        {
            if (player == null)
            {
                return;
            }

            player.LEFTATUTC = utcNow;
            player.ISREADY = false;
        }

        private static void FinalizeMatchWithWinner(
            MATCH match,
            IEnumerable<MATCH_PLAYER> players,
            MATCH_PLAYER winner,
            DateTime utcNow)
        {
            if (match == null || players == null || winner == null)
            {
                return;
            }

            match.STATUSID = MatchStatusIds.FINISHED;
            match.ENDTIME = utcNow;
            match.WINNERUSERID = winner.USERID;

            foreach (MATCH_PLAYER player in players)
            {
                if (player == null)
                {
                    continue;
                }

                player.ISWINNER = player.USERID == winner.USERID;

                if (IsActivePlayer(player))
                {
                    MarkPlayerAsLeft(player, utcNow);
                }
            }
        }

        private static bool HasDuplicateActiveSlot(IReadOnlyCollection<MATCH_PLAYER> activePlayers)
        {
            if (activePlayers == null || activePlayers.Count == 0)
            {
                return false;
            }

            return activePlayers
                .Where(p => p != null)
                .GroupBy(p => p.SLOTNUMBER)
                .Any(g => g.Count() > 1);
        }
    }
}
