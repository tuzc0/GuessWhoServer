using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData
    {
        private static bool IsLobbyMatch(MATCH m) => m != null && m.STATUSID == MATCH_STATUS_LOBBY;
        private static bool IsInProgressMatch(MATCH m) => m != null && m.STATUSID == MATCH_STATUS_IN_PROGRESS;
        private static bool IsActivePlayer(MATCH_PLAYER p) => p != null && p.LEFTATUTC == null;
        private static IQueryable<MATCH_PLAYER> GetActivePlayersForMatch(GuessWhoDBEntities db, long id) => db.MATCH_PLAYER.Where(mp => mp.MATCHID == id && mp.LEFTATUTC == null);
        private static IQueryable<MATCH_PLAYER> GetPlayersForMatch(GuessWhoDBEntities db, long id) => db.MATCH_PLAYER.Where(mp => mp.MATCHID == id);
        private static void MarkPlayerAsLeft(MATCH_PLAYER p, DateTime d) { if (p != null) { p.LEFTATUTC = d; p.ISREADY = false; } }
        private static void FinalizeMatchWithWinner(MATCH m, IEnumerable<MATCH_PLAYER> ps, MATCH_PLAYER w, DateTime d)
        {
            m.STATUSID = MATCH_STATUS_COMPLETED; m.ENDTIME = d; m.WINNERUSERID = w.USERID;
            foreach (var p in ps) { p.ISWINNER = p.USERID == w.USERID; if (IsActivePlayer(p)) MarkPlayerAsLeft(p, d); }
        }
    }
}