using GuessWhoDataAccess.Data.Helpers;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const string SQL_REJOIN_BY_CODE_ATOMIC = @"UPDATE MP SET MP.LEFTATUTC = NULL, MP.ISREADY = 0, MP.JOINEDATUTC = @NowUtc
                                                         FROM MATCH_PLAYER MP
                                                         INNER JOIN MATCH M ON M.MATCHID = MP.MATCHID
                                                         WHERE M.MATCHID = @MatchId
                                                            AND M.MATCHCODE = @MatchCode
                                                            AND M.STATUSID = @MatchStatusLobby
                                                            AND M.STARTTIME IS NULL
                                                            AND M.ENDTIME IS NULL
                                                            AND M.ISCODEJOINENABLED = 1
                                                            AND MP.USERID = @UserId
                                                            AND MP.LEFTATUTC IS NOT NULL
                                                            AND NOT EXISTS (
                                                                SELECT 1
                                                                FROM MATCH_PLAYER MP_OCCUPIER
                                                                WHERE MP_OCCUPIER.MATCHID = MP.MATCHID
                                                                    AND MP_OCCUPIER.SLOTNUMBER = MP.SLOTNUMBER
                                                                    AND MP_OCCUPIER.LEFTATUTC IS NULL
                                                                    AND MP_OCCUPIER.USERID != MP.USERID);";

        private const string SQL_JOIN_BY_CODE_ATOMIC = @";WITH ActivePlayers AS(
                                                            SELECT MP.MATCHID, MP.SLOTNUMBER
                                                            FROM MATCH_PLAYER MP
                                                            WHERE MP.MATCHID = @MatchId
                                                                AND MP.LEFTATUTC IS NULL),
                                                            SlotPick AS(
                                                               SELECT
                                                               CASE
                                                               WHEN EXISTS (SELECT 1 FROM ActivePlayers WHERE SLOTNUMBER = @HostSlot) THEN @GuestSlot
                                                                    ELSE @HostSlot
                                                               END AS SlotToUse,
                                                               (SELECT COUNT(*) FROM ActivePlayers) AS ActiveCount,
                                                               (SELECT COUNT(DISTINCT SLOTNUMBER) FROM ActivePlayers) AS DistinctSlots)
                                                            INSERT INTO MATCH_PLAYER (MATCHID, USERID, SLOTNUMBER, ISHOST, ISREADY, JOINEDATUTC)
                                                            SELECT
                                                                @MatchId,
                                                                @UserId,
                                                                SP.SlotToUse,
                                                                CASE WHEN SP.SlotToUse = @HostSlot THEN 1 ELSE 0 END,
                                                                0,
                                                                @NowUtc
                                                            FROM SlotPick SP
                                                            INNER JOIN MATCH M ON M.MATCHID = @MatchId
                                                            WHERE M.MATCHCODE = @MatchCode
                                                                AND M.STATUSID = @MatchStatusLobby
                                                                AND M.STARTTIME IS NULL
                                                                AND M.ENDTIME IS NULL
                                                                AND M.ISCODEJOINENABLED = 1
                                                                AND SP.ActiveCount < @MaxPlayers
                                                                AND SP.ActiveCount = SP.DistinctSlots
                                                                AND NOT EXISTS (
                                                                    SELECT 1
                                                                    FROM MATCH_PLAYER MPX
                                                                    WHERE MPX.MATCHID = @MatchId
                                                                        AND MPX.USERID = @UserId
                                                                        AND MPX.LEFTATUTC IS NULL);";

        private const string SQL_JOIN_PUBLIC_BY_ID_ATOMIC = @";WITH ActivePlayers AS(
                                                                SELECT MP.MATCHID, MP.SLOTNUMBER
                                                                FROM MATCH_PLAYER MP
                                                                WHERE MP.MATCHID = @MatchId
                                                                    AND MP.LEFTATUTC IS NULL),
                                                            SlotPick AS(
                                                                SELECT
                                                                CASE
                                                                    WHEN EXISTS (SELECT 1 FROM ActivePlayers WHERE SLOTNUMBER = @HostSlot) THEN @GuestSlot
                                                                    ELSE @HostSlot
                                                                END AS SlotToUse,
                                                                (SELECT COUNT(*) FROM ActivePlayers) AS ActiveCount,
                                                                (SELECT COUNT(DISTINCT SLOTNUMBER) FROM ActivePlayers) AS DistinctSlots)
                                                            INSERT INTO MATCH_PLAYER (MATCHID, USERID, SLOTNUMBER, ISHOST, ISREADY, JOINEDATUTC)
                                                            SELECT
                                                                @MatchId,
                                                                @UserId,
                                                                SP.SlotToUse,
                                                                CASE WHEN SP.SlotToUse = @HostSlot THEN 1 ELSE 0 END,
                                                                0,
                                                                @NowUtc
                                                            FROM SlotPick SP
                                                            INNER JOIN MATCH M ON M.MATCHID = @MatchId
                                                            WHERE M.STATUSID = @MatchStatusLobby
                                                                AND M.VISIBILITYID = @VisibilityPublic
                                                                AND M.STARTTIME IS NULL
                                                                AND M.ENDTIME IS NULL
                                                                AND SP.ActiveCount < @MaxPlayers
                                                                AND SP.ActiveCount = SP.DistinctSlots
                                                                AND NOT EXISTS (
                                                                    SELECT 1
                                                                    FROM MATCH_PLAYER MPX
                                                                    WHERE MPX.MATCHID = @MatchId
                                                                        AND MPX.USERID = @UserId
                                                                        AND MPX.LEFTATUTC IS NULL);";

        private sealed class JoinMatchDiagnostic
        {
            public bool MatchExists { get; set; }
            public byte MatchStatusId { get; set; }
            public bool IsCodeJoinEnabled { get; set; }
            public bool HasStartTime { get; set; }
            public bool HasEndTime { get; set; }
            public int ActiveCount { get; set; }
            public int DistinctSlots { get; set; }
            public bool IsAlreadyActiveInMatch { get; set; }
            public bool IsMatchCodeCorrect { get; set; } 
        }

        public JoinMatchResult AddPlayerToMatchByCode(JoinMatchArgs matchArgs)
        {
            if (matchArgs == null)
            {
                throw new ArgumentNullException(nameof(matchArgs));
            }

            if (IsJoinArgsInvalid(matchArgs))
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotJoinable, matchArgs.MatchId);
            }

            if (IsInOtherActiveMatch(matchArgs))
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.InOtherActiveMatch, matchArgs.MatchId);
            }

            DateTime utcNow = DateTime.UtcNow;

            JoinAttemptOutcome joinAttemptOutcome = TryJoinOrRejoinAtomic(matchArgs, utcNow);

            if (joinAttemptOutcome.IsSuccess)
            {
                return JoinMatchResult.Success(matchArgs.MatchId);
            }

            if (joinAttemptOutcome.IsGuestSlotTaken)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.GuestSlotTaken, matchArgs.MatchId);
            }

            JoinMatchDiagnostic joinMatchDiagnostic = LoadJoinDiagnostic(matchArgs);

            return BuildJoinResultFromDiagnostic(matchArgs, joinMatchDiagnostic);
        }

        private static bool IsJoinArgsInvalid(JoinMatchArgs matchArgs)
        {
            return matchArgs.MatchId <= 0 ||
                   matchArgs.UserProfileId <= 0 ||
                   string.IsNullOrWhiteSpace(matchArgs.MatchCode);
        }

        private bool IsInOtherActiveMatch(JoinMatchArgs matchArgs)
        {
            return dataContext.MATCH_PLAYER.Any(matchPlayerEntity =>
                matchPlayerEntity.USERID == matchArgs.UserProfileId &&
                matchPlayerEntity.LEFTATUTC == null &&
                matchPlayerEntity.MATCHID != matchArgs.MatchId &&
                matchPlayerEntity.MATCH.STATUSID != MatchStatusIds.FINISHED &&
                matchPlayerEntity.MATCH.STATUSID != MatchStatusIds.CANCELLED);
        }

        private readonly struct JoinAttemptOutcome
        {
            public bool IsSuccess { get; }
            public bool IsGuestSlotTaken { get; }

            private JoinAttemptOutcome(bool isSuccess, bool isGuestSlotTaken)
            {
                IsSuccess = isSuccess;
                IsGuestSlotTaken = isGuestSlotTaken;
            }

            public static JoinAttemptOutcome Success()
            {
                return new JoinAttemptOutcome(isSuccess: true, isGuestSlotTaken: false);
            }

            public static JoinAttemptOutcome GuestSlotTaken()
            {
                return new JoinAttemptOutcome(isSuccess: false, isGuestSlotTaken: true);
            }

            public static JoinAttemptOutcome Failed()
            {
                return new JoinAttemptOutcome(isSuccess: false, isGuestSlotTaken: false);
            }
        }

        private JoinAttemptOutcome TryJoinOrRejoinAtomic(JoinMatchArgs matchArgs, DateTime utcNow)
        {
            try
            {
                int rejoinRowsAffected = ExecuteRejoinByCode(matchArgs, utcNow);

                if (rejoinRowsAffected > 0)
                {
                    return JoinAttemptOutcome.Success();
                }

                int joinRowsAffected = ExecuteJoinByCode(matchArgs, utcNow);

                if (joinRowsAffected > 0)
                {
                    return JoinAttemptOutcome.Success();
                }

                return JoinAttemptOutcome.Failed();
            }
            catch (SqlException ex) when (SqlExceptionInspector.IsUniqueConstraintViolation(ex))
            {
                return JoinAttemptOutcome.GuestSlotTaken();
            }
        }

        private int ExecuteRejoinByCode(JoinMatchArgs matchArgs, DateTime utcNow)
        {
            string matchCode = NormalizeMatchCode(matchArgs.MatchCode); 

            return dataContext.Database.ExecuteSqlCommand(
                SQL_REJOIN_BY_CODE_ATOMIC,
                new SqlParameter("@MatchId", matchArgs.MatchId),
                new SqlParameter("@MatchCode", matchCode), 
                new SqlParameter("@UserId", matchArgs.UserProfileId),
                new SqlParameter("@NowUtc", utcNow),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY));
        }

        private int ExecuteJoinByCode(JoinMatchArgs matchArgs, DateTime utcNow)
        {
            string matchCode = NormalizeMatchCode(matchArgs.MatchCode); 

            return dataContext.Database.ExecuteSqlCommand(
                SQL_JOIN_BY_CODE_ATOMIC,
                new SqlParameter("@MatchId", matchArgs.MatchId),
                new SqlParameter("@MatchCode", matchCode), 
                new SqlParameter("@UserId", matchArgs.UserProfileId),
                new SqlParameter("@NowUtc", utcNow),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY),
                new SqlParameter("@MaxPlayers", MAX_PLAYERS_BY_SCHEMA),
                new SqlParameter("@HostSlot", HOST_SLOT_NUMBER),
                new SqlParameter("@GuestSlot", GUEST_SLOT_NUMBER));
        }

        private JoinMatchDiagnostic LoadJoinDiagnostic(JoinMatchArgs matchArgs)
        {
            string matchCode = NormalizeMatchCode(matchArgs.MatchCode); 

            return
                (from matchEntity in dataContext.MATCH.AsNoTracking()
                 where matchEntity.MATCHID == matchArgs.MatchId
                 select new JoinMatchDiagnostic
                 {
                     MatchExists = true,
                     MatchStatusId = matchEntity.STATUSID,
                     IsCodeJoinEnabled = matchEntity.ISCODEJOINENABLED,
                     HasStartTime = matchEntity.STARTTIME != null,
                     HasEndTime = matchEntity.ENDTIME != null,
                     ActiveCount = dataContext.MATCH_PLAYER.Count(p => p.MATCHID == matchArgs.MatchId && p.LEFTATUTC == null),
                     DistinctSlots = dataContext.MATCH_PLAYER
                         .Where(p => p.MATCHID == matchArgs.MatchId && p.LEFTATUTC == null)
                         .Select(p => p.SLOTNUMBER)
                         .Distinct()
                         .Count(),
                     IsAlreadyActiveInMatch = dataContext.MATCH_PLAYER.Any(p =>
                         p.MATCHID == matchArgs.MatchId &&
                         p.USERID == matchArgs.UserProfileId &&
                         p.LEFTATUTC == null),
                     IsMatchCodeCorrect = matchEntity.MATCHCODE == matchCode 
                 })
                .SingleOrDefault();
        }

        private JoinMatchResult BuildJoinResultFromDiagnostic(JoinMatchArgs matchArgs, JoinMatchDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotFound, matchArgs.MatchId);
            }

            if (!diagnostic.IsMatchCodeCorrect) 
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotFound, matchArgs.MatchId);
            }

            bool isMatchNotJoinable =
                diagnostic.MatchStatusId != MatchStatusIds.LOBBY ||
                diagnostic.HasStartTime ||
                diagnostic.HasEndTime ||
                !diagnostic.IsCodeJoinEnabled;

            if (isMatchNotJoinable)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotJoinable, matchArgs.MatchId);
            }

            if (diagnostic.IsAlreadyActiveInMatch)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.PlayerAlreadyInMatch, matchArgs.MatchId);
            }

            if (diagnostic.ActiveCount >= MAX_PLAYERS_BY_SCHEMA)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.GuestSlotTaken, matchArgs.MatchId);
            }

            bool hasSlotInconsistency = diagnostic.ActiveCount != diagnostic.DistinctSlots;

            if (hasSlotInconsistency)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.OperationConflict, matchArgs.MatchId);
            }

            return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotJoinable, matchArgs.MatchId);
        }

        private sealed class PublicJoinDiagnostic
        {
            public bool MatchExists { get; set; }
            public byte MatchStatusId { get; set; }
            public byte VisibilityId { get; set; }
            public bool HasStartTime { get; set; }
            public bool HasEndTime { get; set; }
            public int ActiveCount { get; set; }
            public int DistinctSlots { get; set; }
            public bool IsAlreadyActiveInMatch { get; set; }
        }

        public JoinMatchResult AddPlayerToPublicMatchById(long matchId, long userProfileId)
        {
            if (matchId <= 0 || userProfileId <= 0)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.InvalidArgs, matchId);
            }

            bool isInOtherActiveMatch = dataContext.MATCH_PLAYER.Any(mp =>
                mp.USERID == userProfileId &&
                mp.LEFTATUTC == null &&
                mp.MATCHID != matchId &&
                mp.MATCH.STATUSID != MatchStatusIds.FINISHED &&
                mp.MATCH.STATUSID != MatchStatusIds.CANCELLED);

            if (isInOtherActiveMatch)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.InOtherActiveMatch, matchId);
            }

            DateTime utcNow = DateTime.UtcNow;

            try
            {
                int rowsAffected = dataContext.Database.ExecuteSqlCommand(
                    SQL_JOIN_PUBLIC_BY_ID_ATOMIC,
                    new SqlParameter("@MatchId", matchId),
                    new SqlParameter("@UserId", userProfileId),
                    new SqlParameter("@NowUtc", utcNow),
                    new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY),
                    new SqlParameter("@VisibilityPublic", MatchVisibilityIds.PUBLIC),
                    new SqlParameter("@MaxPlayers", MAX_PLAYERS_BY_SCHEMA),
                    new SqlParameter("@HostSlot", HOST_SLOT_NUMBER),
                    new SqlParameter("@GuestSlot", GUEST_SLOT_NUMBER));

                if (rowsAffected > 0)
                {
                    return JoinMatchResult.Success(matchId);
                }
            }
            catch (SqlException ex) when (SqlExceptionInspector.IsUniqueConstraintViolation(ex))
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.GuestSlotTaken, matchId);
            }

            PublicJoinDiagnostic diagnostic =
                (from matchEntity in dataContext.MATCH.AsNoTracking()
                 where matchEntity.MATCHID == matchId
                 select new PublicJoinDiagnostic
                 {
                     MatchExists = true,
                     MatchStatusId = matchEntity.STATUSID,
                     VisibilityId = matchEntity.VISIBILITYID,
                     HasStartTime = matchEntity.STARTTIME != null,
                     HasEndTime = matchEntity.ENDTIME != null,
                     ActiveCount = dataContext.MATCH_PLAYER.Count(p => p.MATCHID == matchId && p.LEFTATUTC == null),
                     DistinctSlots = dataContext.MATCH_PLAYER
                         .Where(p => p.MATCHID == matchId && p.LEFTATUTC == null)
                         .Select(p => p.SLOTNUMBER)
                         .Distinct()
                         .Count(),
                     IsAlreadyActiveInMatch = dataContext.MATCH_PLAYER.Any(p =>
                         p.MATCHID == matchId &&
                         p.USERID == userProfileId &&
                         p.LEFTATUTC == null)
                 })
                .SingleOrDefault();

            if (diagnostic == null)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotFound, matchId);
            }

            bool isNotJoinable =
                diagnostic.MatchStatusId != MatchStatusIds.LOBBY ||
                diagnostic.VisibilityId != MatchVisibilityIds.PUBLIC ||
                diagnostic.HasStartTime ||
                diagnostic.HasEndTime;

            if (isNotJoinable)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotJoinable, matchId);
            }

            if (diagnostic.IsAlreadyActiveInMatch)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.PlayerAlreadyInMatch, matchId);
            }

            if (diagnostic.ActiveCount >= MAX_PLAYERS_BY_SCHEMA)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.GuestSlotTaken, matchId);
            }

            if (diagnostic.ActiveCount != diagnostic.DistinctSlots)
            {
                return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotJoinable, matchId);
            }

            return JoinMatchResult.Fail(JoinMatchResultCode.MatchNotJoinable, matchId);
        }

        public LeaveMatchResult LeaveMatch(MatchPlayerArgs playerArgs)
        {
            if (playerArgs == null)
            {
                throw new ArgumentNullException(nameof(playerArgs));
            }

            if (playerArgs.MatchId <= 0 || playerArgs.UserProfileId <= 0)
            {
                return LeaveMatchResult.Fail(LeaveMatchResultCode.PlayerNotInMatch);
            }

            MATCH matchEntity = dataContext.MATCH
                .Include(m => m.MATCH_PLAYER)
                .SingleOrDefault(m => m.MATCHID == playerArgs.MatchId);

            if (matchEntity == null)
            {
                return LeaveMatchResult.Fail(LeaveMatchResultCode.MatchNotFound);
            }

            List<MATCH_PLAYER> matchPlayers = (matchEntity.MATCH_PLAYER ?? new List<MATCH_PLAYER>()).ToList();

            MATCH_PLAYER playerEntity = matchPlayers
                .SingleOrDefault(p => p != null && p.USERID == playerArgs.UserProfileId);

            if (playerEntity == null)
            {
                return LeaveMatchResult.Fail(LeaveMatchResultCode.PlayerNotInMatch);
            }

            if (playerEntity.LEFTATUTC != null)
            {
                return LeaveMatchResult.Fail(LeaveMatchResultCode.PlayerAlreadyLeft);
            }

            DateTime utcNow = DateTime.UtcNow;

            MarkPlayerAsLeft(playerEntity, utcNow);

            if (playerEntity.ISHOST && !IsCompletedMatch(matchEntity))
            {
                matchEntity.STATUSID = MatchStatusIds.CANCELLED;
                matchEntity.ENDTIME = utcNow;

                foreach (MATCH_PLAYER activePlayer in matchPlayers.Where(p => p != null && p.LEFTATUTC == null))
                {
                    MarkPlayerAsLeft(activePlayer, utcNow);
                }
            }

            return LeaveMatchResult.Success();
        }

        public KickPlayerResult KickPlayer(KickPlayerArgs playerArgs)
        {
            if (playerArgs == null)
            {
                throw new ArgumentNullException(nameof(playerArgs));
            }

            if (playerArgs.MatchId <= 0 || playerArgs.UserProfileId <= 0 || playerArgs.TargetUserId <= 0)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.MatchNotFound);
            }

            MATCH matchEntity = dataContext.MATCH
                .Include(m => m.MATCH_PLAYER)
                .SingleOrDefault(m => m.MATCHID == playerArgs.MatchId);

            if (matchEntity == null)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.MatchNotFound);
            }

            if (!IsLobbyMatch(matchEntity))
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.MatchNotInLobby);
            }

            List<MATCH_PLAYER> matchPlayers = (matchEntity.MATCH_PLAYER ?? new List<MATCH_PLAYER>()).ToList();

            MATCH_PLAYER hostPlayerEntity = matchPlayers
                .SingleOrDefault(p => p != null && p.USERID == playerArgs.UserProfileId);

            if (hostPlayerEntity == null || hostPlayerEntity.LEFTATUTC != null)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.HostNotInMatch);
            }

            if (!hostPlayerEntity.ISHOST)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.HostNotAuthorized);
            }

            MATCH_PLAYER targetPlayerEntity = matchPlayers
                .SingleOrDefault(p => p != null && p.USERID == playerArgs.TargetUserId);

            if (targetPlayerEntity == null)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.TargetNotInMatch);
            }

            if (targetPlayerEntity.LEFTATUTC != null)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.TargetAlreadyLeft);
            }

            if (targetPlayerEntity.ISHOST)
            {
                return KickPlayerResult.Fail(KickPlayerResultCode.CannotKickHost);
            }

            DateTime utcNow = DateTime.UtcNow;

            MarkPlayerAsLeft(targetPlayerEntity, utcNow);

            return KickPlayerResult.Success();
        }
    }
}
