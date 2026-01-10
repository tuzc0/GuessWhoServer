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

        private sealed class MatchRow
        {
            public long MatchId { get; set; }
            public string MatchCode { get; set; }
            public byte StatusId { get; set; }
            public byte VisibilityId { get; set; }
            public byte ModeId { get; set; }
            public DateTime CreatedAtUtc { get; set; }
        }

        private sealed class LobbyPlayerRow
        {
            public long MatchId { get; set; }
            public long UserId { get; set; }
            public string DisplayName { get; set; }
            public string AvatarId { get; set; }
            public int SlotNumber { get; set; }
            public bool IsReady { get; set; }
            public bool IsHost { get; set; }
        }

        public MatchSnapshot GetOpenMatchByCode(string matchCode)
        {
            string safeCode = NormalizeMatchCode(matchCode);

            if (string.IsNullOrWhiteSpace(safeCode))
            {
                return MatchSnapshot.CreateInvalid();
            }

            var rawData = dataContext.MATCH
                .AsNoTracking()
                .Where(m =>
                    m.MATCHCODE == safeCode &&
                    m.STATUSID == MatchStatusIds.LOBBY &&
                    m.STARTTIME == null &&
                    m.ENDTIME == null &&
                    m.ISCODEJOINENABLED)
                .Select(m => new
                {
                    m.MATCHID,
                    m.MATCHCODE,
                    m.STATUSID,
                    m.VISIBILITYID,
                    m.MODEID,
                    m.CREATEDATUTC
                })
                .FirstOrDefault();

            if (rawData == null)
            {
                return MatchSnapshot.CreateInvalid();
            }

            var matchSnapshot = new MatchSnapshot(
                rawData.MATCHID,
                rawData.MATCHCODE ?? string.Empty,
                rawData.STATUSID,
                rawData.VISIBILITYID,
                rawData.MODEID,
                rawData.CREATEDATUTC);

            return matchSnapshot.IsValid
                ? matchSnapshot
                : MatchSnapshot.CreateInvalid();
        }

        public IReadOnlyList<MatchSnapshot> GetPublicLobbyMatches()
        {
            List<MatchRow> rows = dataContext.MATCH
                .AsNoTracking()
                .Where(m =>
                    m.VISIBILITYID == MatchVisibilityIds.PUBLIC &&
                    m.STATUSID == MatchStatusIds.LOBBY &&
                    m.STARTTIME == null &&
                    m.ENDTIME == null)
                .OrderByDescending(m => m.CREATEDATUTC)
                .Select(m => new MatchRow
                {
                    MatchId = m.MATCHID,
                    MatchCode = m.MATCHCODE,
                    StatusId = m.STATUSID,
                    VisibilityId = m.VISIBILITYID,
                    ModeId = m.MODEID,
                    CreatedAtUtc = m.CREATEDATUTC
                })
                .ToList();

            if (rows.Count == 0)
            {
                return Array.Empty<MatchSnapshot>();
            }

            var result = new List<MatchSnapshot>(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                MatchRow row = rows[i];

                result.Add(new MatchSnapshot(
                    row.MatchId,
                    row.MatchCode ?? string.Empty,
                    row.StatusId,
                    row.VisibilityId,
                    row.ModeId,
                    row.CreatedAtUtc));
            }

            return result;
        }

        public MatchSnapshot GetMatchById(long matchId)
        {
            if (matchId <= 0)
            {
                return MatchSnapshot.CreateInvalid();
            }

            MatchRow row = dataContext.MATCH
                .AsNoTracking()
                .Where(m => m.MATCHID == matchId)
                .Select(m => new MatchRow
                {
                    MatchId = m.MATCHID,
                    MatchCode = m.MATCHCODE,
                    StatusId = m.STATUSID,
                    VisibilityId = m.VISIBILITYID,
                    ModeId = m.MODEID,
                    CreatedAtUtc = m.CREATEDATUTC
                })
                .FirstOrDefault();

            if (row == null)
            {
                return MatchSnapshot.CreateInvalid();
            }

            MatchSnapshot snapshot = new MatchSnapshot(
                row.MatchId,
                row.MatchCode ?? string.Empty,
                row.StatusId,
                row.VisibilityId,
                row.ModeId,
                row.CreatedAtUtc);

            return snapshot.IsValid ? snapshot : MatchSnapshot.CreateInvalid();
        }

        public IReadOnlyList<LobbyPlayerSnapshot> GetMatchPlayers(long matchId)
        {
            if (matchId <= 0)
            {
                return Array.Empty<LobbyPlayerSnapshot>();
            }

            List<LobbyPlayerRow> rows =
                (from matchPlayerEntity in dataContext.MATCH_PLAYER.AsNoTracking()
                 join userProfileEntity in dataContext.USER_PROFILE.AsNoTracking()
                    on matchPlayerEntity.USERID equals userProfileEntity.USERID
                 join accountEntity in dataContext.ACCOUNT.AsNoTracking()
                    on userProfileEntity.USERID equals accountEntity.USERID
                 where matchPlayerEntity.MATCHID == matchId &&
                       matchPlayerEntity.LEFTATUTC == null &&
                       !accountEntity.ISDELETED
                 orderby matchPlayerEntity.SLOTNUMBER
                 select new LobbyPlayerRow
                 {
                     MatchId = matchPlayerEntity.MATCHID,
                     UserId = matchPlayerEntity.USERID,
                     DisplayName = userProfileEntity.DISPLAYNAME,
                     AvatarId = userProfileEntity.AVATARID,
                     SlotNumber = (int)matchPlayerEntity.SLOTNUMBER,
                     IsReady = matchPlayerEntity.ISREADY,
                     IsHost = matchPlayerEntity.ISHOST
                 })
                .ToList();

            if (rows.Count == 0)
            {
                return Array.Empty<LobbyPlayerSnapshot>();
            }

            var result = new List<LobbyPlayerSnapshot>(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                LobbyPlayerRow row = rows[i];

                result.Add(new LobbyPlayerSnapshot(
                    row.MatchId,
                    row.UserId,
                    row.DisplayName ?? string.Empty,
                    row.AvatarId ?? string.Empty,
                    (byte)row.SlotNumber,
                    row.IsReady,
                    row.IsHost));
            }

            return result;
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

            return rows.Count == 0 ? Array.Empty<long>() : rows;
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
