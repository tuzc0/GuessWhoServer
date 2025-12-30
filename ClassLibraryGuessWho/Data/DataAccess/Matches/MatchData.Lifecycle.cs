using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Models.Matches;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Data.Entity.Infrastructure;

namespace ClassLibraryGuessWho.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const string SQL_START_MATCH_ATOMIC = @"UPDATE M SET M.STATUSID = @MatchStatusActive, M.STARTTIME = @NowUtc
                                                      FROM MATCH M
                                                      WHERE M.MATCHID = @MatchId
                                                      AND M.STATUSID = @MatchStatusLobby
                                                      AND M.STARTTIME IS NULL
                                                      AND M.ENDTIME IS NULL
                                                      AND EXISTS (
                                                        SELECT 1
                                                        FROM MATCH_PLAYER MP
                                                        WHERE MP.MATCHID = M.MATCHID
                                                        AND MP.LEFTATUTC IS NULL
                                                        GROUP BY MP.MATCHID
                                                        HAVING COUNT(*) = @ExpectedPlayers
                                                            AND SUM(CASE WHEN MP.ISREADY = 1 THEN 1 ELSE 0 END) = @ExpectedPlayers
                                                            AND COUNT(DISTINCT MP.SLOTNUMBER) = @ExpectedPlayers
                                                            AND MIN(MP.SLOTNUMBER) = @HostSlotNumber
                                                            AND MAX(MP.SLOTNUMBER) = @GuestSlotNumber);";

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

            try
            {
                var matchEntity = new MATCH
                {
                    VISIBILITYID = matchArgs.Visibility,
                    STATUSID = matchArgs.MatchStatus,
                    MODEID = matchArgs.Mode,
                    MATCHCODE = matchArgs.MatchCode,
                    CREATEDATUTC = matchArgs.CreateDate,
                    ISCODEJOINENABLED = true
                };

                dataContext.MATCH.Add(matchEntity);

                dataContext.MATCH_PLAYER.Add(new MATCH_PLAYER
                {
                    MATCH = matchEntity,
                    USERID = matchArgs.UserProfileId,
                    SLOTNUMBER = HOST_SLOT_NUMBER,
                    ISHOST = true,
                    ISREADY = true,
                    JOINEDATUTC = matchArgs.CreateDate
                });

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

        public StartMatchResult StartMatch(long matchId)
        {
            if (matchId <= 0)
            {
                return StartMatchResult.Fail(StartMatchResultCode.MatchNotFound);
            }

            DateTime nowUtc = DateTime.UtcNow;

            int affectedRows = dataContext.Database.ExecuteSqlCommand(
                SQL_START_MATCH_ATOMIC,
                new SqlParameter("@MatchId", matchId),
                new SqlParameter("@NowUtc", nowUtc),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY),
                new SqlParameter("@MatchStatusActive", MatchStatusIds.ACTIVE),
                new SqlParameter("@ExpectedPlayers", MIN_PLAYERS_TO_START),
                new SqlParameter("@HostSlotNumber", HOST_SLOT_NUMBER),
                new SqlParameter("@GuestSlotNumber", GUEST_SLOT_NUMBER));

            if (affectedRows > 0)
            {
                return StartMatchResult.Success();
            }

            StartMatchDiagnostic diagnostic = (from matchEntity in dataContext.MATCH.AsNoTracking()
                                               where matchEntity.MATCHID == matchId
                                               select new StartMatchDiagnostic
                                               {
                                                   MatchStatusId = matchEntity.STATUSID,
                                                   ActivePlayersCount = dataContext.MATCH_PLAYER.Count(p =>
                                                   p.MATCHID == matchId &&
                                                   p.LEFTATUTC == null),
                                                   DistinctSlotCount = dataContext.MATCH_PLAYER
                                                   .Where(p => p.MATCHID == matchId && p.LEFTATUTC == null)
                                                   .Select(p => p.SLOTNUMBER)
                                                   .Distinct()
                                                   .Count(),
                                                   NotReadyCount = dataContext.MATCH_PLAYER.Count(p =>
                                                   p.MATCHID == matchId &&
                                                   p.LEFTATUTC == null &&
                                                   !p.ISREADY),
                                                   HasHostSlot = dataContext.MATCH_PLAYER.Any(p =>
                                                   p.MATCHID == matchId &&
                                                   p.LEFTATUTC == null &&
                                                   p.SLOTNUMBER == HOST_SLOT_NUMBER),
                                                   HasGuestSlot = dataContext.MATCH_PLAYER.Any(p =>
                                                   p.MATCHID == matchId &&
                                                   p.LEFTATUTC == null &&
                                                   p.SLOTNUMBER == GUEST_SLOT_NUMBER)
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

            if (diagnostic.ActivePlayersCount < MIN_PLAYERS_TO_START ||
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

            return StartMatchResult.Fail(StartMatchResultCode.TechnicalError);
        }

        public EndMatchResult EndMatch(EndMatchArgs matchArgs)
        {
            if (matchArgs == null)
            {
                throw new ArgumentNullException(nameof(matchArgs));
            }

            if (matchArgs.MatchId <= 0)
            {
                return EndMatchResult.Fail(EndMatchResultCode.MatchNotFound);
            }

            if (matchArgs.WinnerUserId <= 0)
            {
                return EndMatchResult.Fail(EndMatchResultCode.WinnerNotInMatch);
            }

            MATCH matchEntity = dataContext.MATCH
                .Include(m => m.MATCH_PLAYER)
                .SingleOrDefault(m => m.MATCHID == matchArgs.MatchId);

            if (matchEntity == null)
            {
                return EndMatchResult.Fail(EndMatchResultCode.MatchNotFound);
            }

            if (!IsActiveMatch(matchEntity))
            {
                return EndMatchResult.Fail(EndMatchResultCode.MatchNotInProgress);
            }

            List<MATCH_PLAYER> players = (matchEntity.MATCH_PLAYER ?? new List<MATCH_PLAYER>()).ToList();

            MATCH_PLAYER winnerPlayer = players.SingleOrDefault(p => p != null && p.USERID == matchArgs.WinnerUserId);

            if (winnerPlayer == null)
            {
                return EndMatchResult.Fail(EndMatchResultCode.WinnerNotInMatch);
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

            dataContext.SaveChanges();

            return EndMatchResult.Success();
        }

        public bool AreAllSecretCharactersChosen(long matchId)
        {
            if (matchId <= 0)
            {
                return false;
            }

            var activePlayers = GetActivePlayersForMatch(dataContext, matchId).ToList();

            return activePlayers.Any() &&
                   activePlayers.All(p => p != null && !string.IsNullOrWhiteSpace(p.SECRETCHARACTERID));
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

            HashSet<long> cancelledMatchIds = new HashSet<long>();

            ApplyForceLeaveRules(
                activeEntriesWithMatch,
                activePlayersByMatchId,
                cancelledMatchIds,
                utcNow);

            dataContext.SaveChanges();

            return true;
        }

        private List<PlayerWithMatch> LoadActiveEntriesWithMatches(long userId)
        {
            return(from matchPlayerEntity in dataContext.MATCH_PLAYER
                   join matchEntity in dataContext.MATCH on matchPlayerEntity.MATCHID equals matchEntity.MATCHID into matchJoin
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

        private void ApplyForceLeaveRules(IReadOnlyList<PlayerWithMatch> activeEntriesWithMatch,
            IReadOnlyDictionary<long, List<MATCH_PLAYER>> activePlayersByMatchId, ISet<long> cancelledMatchIds,
            DateTime utcNow)
        {
            if (activeEntriesWithMatch == null)
            {
                throw new ArgumentNullException(nameof(activeEntriesWithMatch));
            }

            if (activePlayersByMatchId == null)
            {
                throw new ArgumentNullException(nameof(activePlayersByMatchId));
            }

            if (cancelledMatchIds == null)
            {
                throw new ArgumentNullException(nameof(cancelledMatchIds));
            }

            foreach (PlayerWithMatch entry in activeEntriesWithMatch)
            {
                ApplyForceLeaveRulesToEntry(entry, activePlayersByMatchId, cancelledMatchIds, utcNow);
            }
        }

        private void ApplyForceLeaveRulesToEntry(PlayerWithMatch entry, IReadOnlyDictionary<long, List<MATCH_PLAYER>> activePlayersByMatchId,
            ISet<long> cancelledMatchIds, DateTime utcNow)
        {
            MATCH_PLAYER playerEntry = entry.Player;
            MATCH matchEntity = entry.Match;

            if (matchEntity == null)
            {
                MarkPlayerAsLeft(playerEntry, utcNow);
                return;
            }

            if (ShouldCancelMatchBecauseHostLeft(playerEntry, matchEntity))
            {
                CancelMatchAndMarkPlayersLeft(matchEntity, playerEntry, activePlayersByMatchId, cancelledMatchIds, utcNow);
                return;
            }

            MarkPlayerLeftIfMatchNotCancelled(matchEntity.MATCHID, playerEntry, cancelledMatchIds, utcNow);
        }

        private static bool ShouldCancelMatchBecauseHostLeft(MATCH_PLAYER playerEntry, MATCH matchEntity)
        {
            return playerEntry.ISHOST && !IsCompletedMatch(matchEntity);
        }

        private void CancelMatchAndMarkPlayersLeft(MATCH matchEntity, MATCH_PLAYER hostPlayerEntry,
            IReadOnlyDictionary<long, List<MATCH_PLAYER>> activePlayersByMatchId, ISet<long> cancelledMatchIds,
            DateTime utcNow)
        {
            if (cancelledMatchIds.Contains(matchEntity.MATCHID))
            {
                return;
            }

            cancelledMatchIds.Add(matchEntity.MATCHID);

            matchEntity.STATUSID = MatchStatusIds.CANCELLED;
            matchEntity.ENDTIME = utcNow;

            if (activePlayersByMatchId.TryGetValue(matchEntity.MATCHID, out List<MATCH_PLAYER> matchActivePlayers))
            {
                foreach (MATCH_PLAYER otherPlayer in matchActivePlayers)
                {
                    MarkPlayerAsLeft(otherPlayer, utcNow);
                }

                return;
            }

            MarkPlayerAsLeft(hostPlayerEntry, utcNow);
        }

        private void MarkPlayerLeftIfMatchNotCancelled(long matchId, MATCH_PLAYER playerEntry, ISet<long> cancelledMatchIds,
            DateTime utcNow)
        {
            if (!cancelledMatchIds.Contains(matchId))
            {
                MarkPlayerAsLeft(playerEntry, utcNow);
            }
        }
    }
}
