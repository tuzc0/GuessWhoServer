using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData
    {
        private static bool IsLobbyMatch(MATCH match)
        {
            return match != null && match.STATUSID == MATCH_STATUS_LOBBY;
        }

        private static bool IsActivePlayer(MATCH_PLAYER player)
        {
            return player != null && player.LEFTATUTC == null;
        }

        private static IQueryable<MATCH_PLAYER> GetActivePlayersForMatch(GuessWhoDBEntities dataBaseContext, long matchId)
        {
            return dataBaseContext.MATCH_PLAYER
                .Where(mp => mp.MATCHID == matchId && mp.LEFTATUTC == null);
        }

        private static void MarkPlayerAsLeft(MATCH_PLAYER player, DateTime leftDateUtc)
        {
            player.LEFTATUTC = leftDateUtc;
            player.ISREADY = false;
        }

        private static bool IsInProgressMatch(MATCH match)
        {
            return match != null && match.STATUSID == MATCH_STATUS_IN_PROGRESS;
        }

        private static IQueryable<MATCH_PLAYER> GetPlayersForMatch(
            GuessWhoDBEntities dataBaseContext,
            long matchId)
        {
            return dataBaseContext.MATCH_PLAYER
                .Where(mp => mp.MATCHID == matchId);
        }

        private static void FinalizeMatchWithWinner(MATCH match, IEnumerable<MATCH_PLAYER> players,
            MATCH_PLAYER winnerPlayer, DateTime endDateUtc)
        {
            match.STATUSID = MATCH_STATUS_COMPLETED;
            match.ENDTIME = endDateUtc;
            match.WINNERUSERID = winnerPlayer.USERID;

            foreach (var player in players)
            {
                player.ISWINNER = player.USERID == winnerPlayer.USERID;

                if (IsActivePlayer(player))
                {
                    MarkPlayerAsLeft(player, endDateUtc);
                }
            }
        }
    }
}
