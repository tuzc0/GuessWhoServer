using GuessWhoServerDomain.Domain.Models.Matches;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const int MAX_TAKE = 10;

        private const string SQL_GET_ACTIVE_PLAYER_IDS =
            @"SELECT TOP (@TakeMax)
                    MP.USERID AS UserId
              FROM dbo.MATCH_PLAYER MP
              WHERE MP.MATCHID = @MatchId
                AND MP.LEFTATUTC IS NULL
              ORDER BY MP.USERID ASC;";

        public MatchSnapshot GetOpenMatchByCode(string matchCode)
        {
            string safeCode = NormalizeMatchCode(matchCode);

            if (string.IsNullOrWhiteSpace(safeCode)) 
            {
                return MatchSnapshot.CreateInvalid();
            }

            MatchSnapshot matchSnapshot = dataContext.MATCH
                .AsNoTracking()
                .Where(m =>
                    m.MATCHCODE == safeCode &&
                    m.STATUSID == MatchStatusIds.LOBBY &&
                    m.STARTTIME == null &&
                    m.ENDTIME == null &&
                    m.ISCODEJOINENABLED)
                .Select(m => new MatchSnapshot(
                    m.MATCHID,
                    m.MATCHCODE ?? string.Empty,
                    m.STATUSID,
                    m.VISIBILITYID,
                    m.MODEID,
                    m.CREATEDATUTC))
                .FirstOrDefault();

            return matchSnapshot.IsValid
                ? matchSnapshot
                : MatchSnapshot.CreateInvalid();
        }

        public IReadOnlyList<MatchSnapshot> GetPublicLobbyMatches()
        {
            List<MatchSnapshot> matches = dataContext.MATCH
                .AsNoTracking()
                .Where(m =>
                    m.VISIBILITYID == MatchVisibilityIds.PUBLIC &&
                    m.STATUSID == MatchStatusIds.LOBBY &&
                    m.STARTTIME == null &&
                    m.ENDTIME == null)
                .OrderByDescending(m => m.CREATEDATUTC)
                .Select(m => new MatchSnapshot(
                    m.MATCHID,
                    m.MATCHCODE ?? string.Empty,
                    m.STATUSID,
                    m.VISIBILITYID,
                    m.MODEID,
                    m.CREATEDATUTC))
                .ToList();

            return matches;
        }

        public MatchSnapshot GetMatchById(long matchId)
        {
            if (matchId <= 0)
            {
                return MatchSnapshot.CreateInvalid();
            }

            MatchSnapshot snapshot = dataContext.MATCH
                .AsNoTracking()
                .Where(m => m.MATCHID == matchId)
                .Select(m => new MatchSnapshot(
                    m.MATCHID,
                    m.MATCHCODE ?? string.Empty,
                    m.STATUSID,
                    m.VISIBILITYID,
                    m.MODEID,
                    m.CREATEDATUTC))
                .DefaultIfEmpty(MatchSnapshot.CreateInvalid())
                .First();

            return snapshot;
        }

        public IReadOnlyList<LobbyPlayerSnapshot> GetMatchPlayers(long matchId)
        {
            if (matchId <= 0)
            {
                return Array.Empty<LobbyPlayerSnapshot>();
            }

            List<LobbyPlayerSnapshot> players =
                (from matchPlayerEntity in dataContext.MATCH_PLAYER.AsNoTracking()
                 join userProfileEntity in dataContext.USER_PROFILE.AsNoTracking()
                    on matchPlayerEntity.USERID equals userProfileEntity.USERID
                 join accountEntity in dataContext.ACCOUNT.AsNoTracking() 
                    on userProfileEntity.USERID equals accountEntity.USERID 
                 where matchPlayerEntity.MATCHID == matchId &&
                       matchPlayerEntity.LEFTATUTC == null &&
                       !accountEntity.ISDELETED 
                 orderby matchPlayerEntity.SLOTNUMBER 
                 select new LobbyPlayerSnapshot(
                     matchPlayerEntity.MATCHID,
                     matchPlayerEntity.USERID,
                     userProfileEntity.DISPLAYNAME ?? string.Empty,
                     userProfileEntity.AVATARID ?? string.Empty,
                     (byte)matchPlayerEntity.SLOTNUMBER,
                     matchPlayerEntity.ISREADY,
                     matchPlayerEntity.ISHOST))
                .ToList();

            return players;
        }

        public IReadOnlyList<long> GetActivePlayerIds(long matchId, int takeMax)
        {
            if (matchId <= 0)
            {
                return Array.Empty<long>();
            }

            int safeTake = NormalizeTake(takeMax);
            if (safeTake <= 0)
            {
                return Array.Empty<long>();
            }

            List<long> rows = dataContext.Database.SqlQuery<long>(
                    SQL_GET_ACTIVE_PLAYER_IDS,
                    new SqlParameter("@MatchId", matchId),
                    new SqlParameter("@TakeMax", safeTake))
                .ToList();

            if (rows.Count == 0)
            {
                return Array.Empty<long>();
            }

            return rows;
        }

        private static int NormalizeTake(int takeMax)
        {
            if (takeMax <= 0)
            {
                return 0;
            }

            return takeMax > MAX_TAKE ? MAX_TAKE : takeMax;
        }

        private static string NormalizeMatchCode(string matchCode) 
        {
            return (matchCode ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
