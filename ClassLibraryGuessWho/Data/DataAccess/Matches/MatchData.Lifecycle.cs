using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Enums.Matches;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const string SP_START_MATCH_ATOMIC = "usp_Match_StartAtomic";
        private const string SP_HANDLE_DISCONNECT = "usp_Match_HandleDisconnect";

        private const string SQL_EXEC_START_MATCH_ATOMIC =
            "EXEC " + SP_START_MATCH_ATOMIC + " " +
            "@MatchId, @HostUserId, @NowUtc, " +
            "@MatchStatusLobby, @MatchStatusActive, " +
            "@ExpectedPlayers, @HostSlotNumber, @GuestSlotNumber";

        private const string SQL_EXEC_HANDLE_DISCONNECT =
            "EXEC " + SP_HANDLE_DISCONNECT + " " +
            "@UserId, @NowUtc, " +
            "@MatchStatusLobby, @MatchStatusActive, @MatchStatusFinished, @MatchStatusCancelled";

        public MatchSnapshot CreateMatchClassic(CreateMatchArgs matchArgs)
        {
            if (matchArgs == null)
            {
                throw new ArgumentNullException(nameof(matchArgs));
            }

            if (matchArgs.UserProfileId <= 0 || string.IsNullOrWhiteSpace(matchArgs.MatchCode))
            {
                return MatchSnapshot.CreateInvalid();
            }

            MATCH matchEntity = null;
            MATCH_PLAYER hostEntity = null;

            try
            {
                matchEntity = new MATCH
                {
                    VISIBILITYID = (byte)matchArgs.Visibility,
                    STATUSID = (byte)matchArgs.MatchStatus,
                    MODEID = (byte)matchArgs.Mode,
                    MATCHCODE = matchArgs.MatchCode,
                    CREATEDATUTC = matchArgs.CreateDate,
                    ISCODEJOINENABLED = true
                };

                hostEntity = new MATCH_PLAYER
                {
                    MATCH = matchEntity,
                    USERID = matchArgs.UserProfileId,
                    SLOTNUMBER = HOST_SLOT_NUMBER,
                    ISHOST = true,
                    ISREADY = true,
                    JOINEDATUTC = matchArgs.CreateDate
                };

                dataContext.MATCH.Add(matchEntity);
                dataContext.MATCH_PLAYER.Add(hostEntity);

                dataContext.SaveChanges();

                return new MatchSnapshot(
                    matchEntity.MATCHID,
                    matchEntity.MATCHCODE ?? string.Empty,
                    matchEntity.STATUSID,
                    matchEntity.VISIBILITYID,
                    matchEntity.MODEID,
                    matchEntity.CREATEDATUTC);
            }
            catch (DbUpdateException ex) when (SqlExceptionInspector.IsUniqueConstraintViolation(ex))
            {
                DetachIfTracked(hostEntity);
                DetachIfTracked(matchEntity);

                return MatchSnapshot.CreateInvalid();
            }
        }

        private sealed class StartMatchDiagnostic
        {
            public byte MatchStatusId { get; set; }
            public int ActivePlayersCount { get; set; }
            public int DistinctSlotCount { get; set; }
            public int NotReadyCount { get; set; }
            public bool HasHostSlot { get; set; }
            public bool HasGuestSlot { get; set; }
        }

        public StartMatchResult StartMatch(long matchId, long hostUserId)
        {
            if (matchId <= 0 || hostUserId <= 0)
            {
                return StartMatchResult.Fail(StartMatchResultCode.MatchNotFound);
            }

            DateTime nowUtc = DateTime.UtcNow;

            int affectedRows = dataContext.Database.ExecuteSqlCommand(
                SQL_EXEC_START_MATCH_ATOMIC,
                new SqlParameter("@MatchId", matchId),
                new SqlParameter("@HostUserId", hostUserId),
                new SqlParameter("@NowUtc", nowUtc),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY),
                new SqlParameter("@MatchStatusActive", MatchStatusIds.ACTIVE),
                new SqlParameter("@ExpectedPlayers", MIN_PLAYERS),
                new SqlParameter("@HostSlotNumber", HOST_SLOT_NUMBER),
                new SqlParameter("@GuestSlotNumber", GUEST_SLOT_NUMBER));

            if (affectedRows > 0)
            {
                return StartMatchResult.Success();
            }

            StartMatchDiagnostic diagnostic =
                (from matchEntity in dataContext.MATCH.AsNoTracking()
                 where matchEntity.MATCHID == matchId
                 select new StartMatchDiagnostic
                 {
                     MatchStatusId = matchEntity.STATUSID,
                     ActivePlayersCount = dataContext.MATCH_PLAYER.Count(p =>
                         p.MATCHID == matchId && p.LEFTATUTC == null),
                     DistinctSlotCount = dataContext.MATCH_PLAYER
                         .Where(p => p.MATCHID == matchId && p.LEFTATUTC == null)
                         .Select(p => p.SLOTNUMBER)
                         .Distinct()
                         .Count(),
                     NotReadyCount = dataContext.MATCH_PLAYER.Count(p =>
                         p.MATCHID == matchId && p.LEFTATUTC == null && !p.ISREADY),
                     HasHostSlot = dataContext.MATCH_PLAYER.Any(p =>
                         p.MATCHID == matchId && p.LEFTATUTC == null && p.SLOTNUMBER == HOST_SLOT_NUMBER),
                     HasGuestSlot = dataContext.MATCH_PLAYER.Any(p =>
                         p.MATCHID == matchId && p.LEFTATUTC == null && p.SLOTNUMBER == GUEST_SLOT_NUMBER)
                 }).SingleOrDefault();

            if (diagnostic == null)
            {
                return StartMatchResult.Fail(StartMatchResultCode.MatchNotFound);
            }

            if (diagnostic.MatchStatusId != MatchStatusIds.LOBBY)
            {
                return StartMatchResult.Fail(StartMatchResultCode.MatchNotInLobby);
            }

            bool hasValidSlots = diagnostic.HasHostSlot && diagnostic.HasGuestSlot;
            bool hasDuplicateSlots = diagnostic.ActivePlayersCount != diagnostic.DistinctSlotCount;

            if (diagnostic.ActivePlayersCount < MIN_PLAYERS ||
                diagnostic.ActivePlayersCount > MAX_PLAYERS_BY_SCHEMA ||
                !hasValidSlots ||
                hasDuplicateSlots)
            {
                return StartMatchResult.Fail(StartMatchResultCode.NotEnoughPlayers);
            }

            if (diagnostic.NotReadyCount > 0)
            {
                return StartMatchResult.Fail(StartMatchResultCode.PlayersNotReady);
            }

            return StartMatchResult.Fail(StartMatchResultCode.ConcurrentUpdate);
        }

        public EndMatchResult EndMatch(EndMatchArgs matchArgs)
        {
            if (matchArgs == null)
            {
                throw new ArgumentNullException(nameof(matchArgs));
            }

            if (matchArgs.MatchId <= 0)
            {
                return EndMatchResult.Fail(EndMatchResultCode.InvalidArgs);
            }

            MATCH matchEntity = dataContext.MATCH
                .Include(m => m.MATCH_PLAYER)
                .SingleOrDefault(m => m.MATCHID == matchArgs.MatchId);

            if (matchEntity == null)
            {
                return EndMatchResult.Fail(EndMatchResultCode.MatchNotFound);
            }

            if (IsCompletedMatch(matchEntity))
            {
                return EndMatchResult.Fail(EndMatchResultCode.AlreadyFinalized);
            }

            if (!IsActiveMatch(matchEntity))
            {
                return EndMatchResult.Fail(EndMatchResultCode.MatchNotInProgress);
            }

            List<MATCH_PLAYER> players = (matchEntity.MATCH_PLAYER ?? new List<MATCH_PLAYER>()).ToList();

            MATCH_PLAYER winnerPlayer = ResolveWinnerPlayer(players, matchArgs.WinnerUserId);

            if (winnerPlayer == null)
            {
                return matchArgs.WinnerUserId > 0
                    ? EndMatchResult.Fail(EndMatchResultCode.WinnerNotInMatch)
                    : EndMatchResult.Fail(EndMatchResultCode.WinnerNotResolvable);
            }

            DateTime utcNow = DateTime.UtcNow;

            matchEntity.STATUSID = MatchStatusIds.FINISHED;
            matchEntity.ENDTIME = utcNow;
            matchEntity.WINNERUSERID = winnerPlayer.USERID;

            foreach (MATCH_PLAYER player in players)
            {
                if (player == null)
                {
                    continue;
                }

                player.ISWINNER = player.USERID == winnerPlayer.USERID;

                if (IsActivePlayer(player))
                {
                    MarkPlayerAsLeft(player, utcNow);
                }
            }

            try
            {
                dataContext.SaveChanges();
                return EndMatchResult.Success(winnerPlayer.USERID);
            }
            catch (DbUpdateConcurrencyException)
            {
                return EndMatchResult.Fail(EndMatchResultCode.ConcurrentUpdate);
            }
        }

        private static MATCH_PLAYER ResolveWinnerPlayer(IReadOnlyList<MATCH_PLAYER> players, long requestedWinnerUserId)
        {
            if (players == null || players.Count == 0)
            {
                return null;
            }

            List<MATCH_PLAYER> activePlayers = players
                .Where(p => p != null && p.LEFTATUTC == null && p.USERID > INVALID_USER)
                .ToList();

            if (requestedWinnerUserId > INVALID_USER)
            {
                return activePlayers.SingleOrDefault(p => p.USERID == requestedWinnerUserId);
            }

            if (activePlayers.Count == 1)
            {
                return activePlayers[0];
            }

            MATCH_PLAYER winnerFlagged = activePlayers.SingleOrDefault(p => p.ISWINNER);
            return winnerFlagged;
        }

        private sealed class PlayerWithMatch
        {
            public MATCH_PLAYER Player { get; }
            public MATCH Match { get; }

            public PlayerWithMatch(MATCH_PLAYER player, MATCH match)
            {
                Player = player ?? throw new ArgumentNullException(nameof(player));
                Match = match;
            }
        }

        public bool ForceLeaveAllMatchesForUser(long userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            DateTime utcNow = DateTime.UtcNow;

            List<PlayerWithMatch> activeEntriesWithMatch = LoadActiveEntriesWithMatches(userId);

            if (!activeEntriesWithMatch.Any())
            {
                return false;
            }

            List<long> matchIdsToCancel = GetMatchIdsToCancel(activeEntriesWithMatch);

            Dictionary<long, List<MATCH_PLAYER>> activePlayersByMatchId =
                LoadActivePlayersByMatchId(matchIdsToCancel);

            var forceLeaveContext = new ForceLeaveContext(activePlayersByMatchId, utcNow);

            ApplyForceLeaveRules(forceLeaveContext, activeEntriesWithMatch);

            dataContext.SaveChanges();

            return true;
        }

        internal enum MatchSqlCode
        {
            OkActiveFinished = 0,
            OkLobbyCancelled = 1,
            OkLeft = 2,

            NotInMatch = 3,
            MatchNotFound = 4,
            OpponentNotFound = 5,
            Conflict = 6,
            AlreadyEnded = 7
        }

        private sealed class DisconnectSqlRow
        {
            public int Code { get; set; }
            public long MatchId { get; set; }
            public byte StatusId { get; set; }
            public long WinnerUserId { get; set; }
        }

        public DisconnectMatchResult HandleDisconnect(long userId, DateTime nowUtc)
        {
            if (userId <= INVALID_USER)
            {
                return DisconnectMatchResult.Fail(DisconnectMatchResultCode.InvalidArgs);
            }

            var parameters = new[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@NowUtc", nowUtc),

                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY),
                new SqlParameter("@MatchStatusActive", MatchStatusIds.ACTIVE),
                new SqlParameter("@MatchStatusFinished", MatchStatusIds.FINISHED),
                new SqlParameter("@MatchStatusCancelled", MatchStatusIds.CANCELLED)
            };

            List<DisconnectSqlRow> rows = dataContext.Database
                .SqlQuery<DisconnectSqlRow>(SQL_EXEC_HANDLE_DISCONNECT, parameters)
                .ToList();

            if (rows.Count == 0)
            {
                return DisconnectMatchResult.Fail(DisconnectMatchResultCode.UnexpectedError);
            }

            DisconnectSqlRow first = rows[0];
            MatchSqlCode sqlCode = (MatchSqlCode)first.Code;

            switch (sqlCode)
            {
                case MatchSqlCode.OkActiveFinished:
                    return new DisconnectMatchResult(
                        DisconnectMatchResultCode.SuccessEndedWithWinner,
                        first.MatchId,
                        first.WinnerUserId);

                case MatchSqlCode.OkLobbyCancelled:
                    return new DisconnectMatchResult(
                        DisconnectMatchResultCode.SuccessCancelledLobby,
                        first.MatchId,
                        INVALID_USER);

                case MatchSqlCode.OkLeft:
                    return new DisconnectMatchResult(
                        DisconnectMatchResultCode.SuccessLeftLobby,
                        first.MatchId,
                        INVALID_USER);

                case MatchSqlCode.AlreadyEnded:
                    if (first.StatusId == MatchStatusIds.FINISHED && first.WinnerUserId > INVALID_USER)
                    {
                        return new DisconnectMatchResult(
                            DisconnectMatchResultCode.AlreadyEndedFinished,
                            first.MatchId,
                            first.WinnerUserId);
                    }

                    if (first.StatusId == MatchStatusIds.CANCELLED)
                    {
                        return new DisconnectMatchResult(
                            DisconnectMatchResultCode.AlreadyEndedCancelled,
                            first.MatchId,
                            INVALID_USER);
                    }

                    return DisconnectMatchResult.Fail(DisconnectMatchResultCode.UnexpectedError);

                case MatchSqlCode.NotInMatch:
                    return DisconnectMatchResult.Fail(DisconnectMatchResultCode.NotInMatch);

                case MatchSqlCode.MatchNotFound:
                    return DisconnectMatchResult.Fail(DisconnectMatchResultCode.MatchNotFound);

                case MatchSqlCode.OpponentNotFound:
                    return DisconnectMatchResult.Fail(DisconnectMatchResultCode.OpponentNotFound);

                case MatchSqlCode.Conflict:
                    return DisconnectMatchResult.Fail(DisconnectMatchResultCode.OperationConflict);

                default:
                    return DisconnectMatchResult.Fail(DisconnectMatchResultCode.UnexpectedError);
            }
        }

        private List<PlayerWithMatch> LoadActiveEntriesWithMatches(long userId)
        {
            return (from matchPlayerEntity in dataContext.MATCH_PLAYER
                    join matchEntity in dataContext.MATCH
                        on matchPlayerEntity.MATCHID equals matchEntity.MATCHID into matchJoin
                    from matchOrNull in matchJoin.DefaultIfEmpty()
                    where matchPlayerEntity.USERID == userId && matchPlayerEntity.LEFTATUTC == null
                    select new PlayerWithMatch(matchPlayerEntity, matchOrNull))
                   .ToList();
        }

        private static List<long> GetMatchIdsToCancel(IReadOnlyList<PlayerWithMatch> activeEntriesWithMatch)
        {
            if (activeEntriesWithMatch == null)
            {
                throw new ArgumentNullException(nameof(activeEntriesWithMatch));
            }

            return activeEntriesWithMatch
                .Where(x => x.Match != null && x.Player.ISHOST && !IsCompletedMatch(x.Match))
                .Select(x => x.Match.MATCHID)
                .Distinct()
                .ToList();
        }

        private Dictionary<long, List<MATCH_PLAYER>> LoadActivePlayersByMatchId(IReadOnlyList<long> matchIdsToCancel)
        {
            if (matchIdsToCancel == null)
            {
                throw new ArgumentNullException(nameof(matchIdsToCancel));
            }

            if (!matchIdsToCancel.Any())
            {
                return new Dictionary<long, List<MATCH_PLAYER>>();
            }

            List<MATCH_PLAYER> activePlayersInCancelableMatches = dataContext.MATCH_PLAYER
                .Where(mp => matchIdsToCancel.Contains(mp.MATCHID) && mp.LEFTATUTC == null)
                .ToList();

            return activePlayersInCancelableMatches
                .GroupBy(p => p.MATCHID)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        private sealed class ForceLeaveContext
        {
            public ForceLeaveContext(
                IReadOnlyDictionary<long, List<MATCH_PLAYER>> activePlayersByMatchId,
                DateTime utcNow)
            {
                ActivePlayersByMatchId = activePlayersByMatchId ??
                    throw new ArgumentNullException(nameof(activePlayersByMatchId));

                CancelledMatchIds = new HashSet<long>();
                UtcNow = utcNow;
            }

            public IReadOnlyDictionary<long, List<MATCH_PLAYER>> ActivePlayersByMatchId { get; }
            public ISet<long> CancelledMatchIds { get; }
            public DateTime UtcNow { get; }
        }

        private void ApplyForceLeaveRules(ForceLeaveContext context, IReadOnlyList<PlayerWithMatch> entries)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            foreach (PlayerWithMatch entry in entries)
            {
                ApplyForceLeaveRulesToEntry(context, entry);
            }
        }

        private void ApplyForceLeaveRulesToEntry(ForceLeaveContext context, PlayerWithMatch entry)
        {
            MATCH_PLAYER playerEntry = entry.Player;
            MATCH matchEntity = entry.Match;

            if (matchEntity == null)
            {
                MarkPlayerAsLeft(playerEntry, context.UtcNow);
                return;
            }

            if (ShouldCancelMatchBecauseHostLeft(playerEntry, matchEntity))
            {
                CancelMatchAndMarkPlayersLeft(context, matchEntity, playerEntry);
                return;
            }

            MarkPlayerLeftIfMatchNotCancelled(context, matchEntity.MATCHID, playerEntry);
        }

        private static bool ShouldCancelMatchBecauseHostLeft(MATCH_PLAYER playerEntry, MATCH matchEntity)
        {
            return playerEntry.ISHOST && !IsCompletedMatch(matchEntity);
        }

        private void CancelMatchAndMarkPlayersLeft(ForceLeaveContext context, MATCH matchEntity, MATCH_PLAYER hostPlayerEntry)
        {
            if (context.CancelledMatchIds.Contains(matchEntity.MATCHID))
            {
                return;
            }

            context.CancelledMatchIds.Add(matchEntity.MATCHID);

            matchEntity.STATUSID = MatchStatusIds.CANCELLED;
            matchEntity.ENDTIME = context.UtcNow;

            if (context.ActivePlayersByMatchId.TryGetValue(matchEntity.MATCHID, out List<MATCH_PLAYER> matchActivePlayers))
            {
                foreach (MATCH_PLAYER otherPlayer in matchActivePlayers)
                {
                    MarkPlayerAsLeft(otherPlayer, context.UtcNow);
                }

                return;
            }

            MarkPlayerAsLeft(hostPlayerEntry, context.UtcNow);
        }

        private void MarkPlayerLeftIfMatchNotCancelled(ForceLeaveContext context, long matchId, MATCH_PLAYER playerEntry)
        {
            if (!context.CancelledMatchIds.Contains(matchId))
            {
                MarkPlayerAsLeft(playerEntry, context.UtcNow);
            }
        }
    }
}
