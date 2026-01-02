using System;
using System.Linq;
using System.Security.Cryptography;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const byte HOST_SLOT_NUMBER = 1;
        private const byte GUEST_SLOT_NUMBER = 2;

        private const int MIN_PLAYERS = 2;
        private const int MAX_PLAYERS_BY_SCHEMA = 2;

        private const string MATCH_CODE_LENGTH = "D6";

        private const long INVALID_MATCH_ID = -1;
        private const long INVALID_USER = 0;

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

        private static bool IsCompletedMatch(MATCH match) =>
            match != null && (match.STATUSID == MatchStatusIds.FINISHED || match.STATUSID == MatchStatusIds.CANCELLED);

        private static bool IsActivePlayer(MATCH_PLAYER player) =>
            player != null && player.LEFTATUTC == null;

        private static IQueryable<MATCH_PLAYER> GetActivePlayersForMatch(GuessWhoDBEntities db, long matchId) =>
            db.MATCH_PLAYER.Where(mp => mp.MATCHID == matchId && mp.LEFTATUTC == null);

        private static void MarkPlayerAsLeft(MATCH_PLAYER player, DateTime utcNow)
        {
            if (player == null)
            {
                return;
            }

            player.LEFTATUTC = utcNow;
            player.ISREADY = false;
        }

        private bool MatchCodeExists(string matchCode)
        {
            string safeCode = (matchCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(safeCode))
            {
                return true;
            }

            return dataContext.MATCH
                .AsNoTracking()
                .Any(m => m.MATCHCODE == matchCode);
        }

        private static string GenerateMatchCode()
        {
            byte[] buffer = new byte[4];

            using (RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create())
            {
                randomGenerator.GetBytes(buffer);
            }

            int randomInt = BitConverter.ToInt32(buffer, 0) & int.MaxValue;

            int codeValue = randomInt % 1000000;

            return codeValue.ToString(MATCH_CODE_LENGTH);
        }
    }
}
