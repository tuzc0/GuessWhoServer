using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public partial class MatchData
    {
        public JoinMatchResult AddPlayerToMatchByCode(JoinMatchArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                MATCH match = GetJoinableMatch(dataBaseContext, args);

                if (match == null)
                {
                    return JoinMatchResult.MatchNotJoinable;
                }

                MATCH_PLAYER existingPlayer = GetExistingPlayer(dataBaseContext, args);

                if (IsExistingActivePlayer(existingPlayer))
                {
                    return JoinMatchResult.PlayerAlreadyInMatch;
                }

                if (IsUserInActiveMatch(dataBaseContext, args.UserProfileId, args.MatchId))
                {
                    return JoinMatchResult.InOtherActiveMatch;
                }

                if (IsGuestSlotTaken(dataBaseContext, args.MatchId, args.UserProfileId))
                {
                    return JoinMatchResult.GuestSlotTaken;
                }

                DateTime joinDateUtc = DateTime.UtcNow;

                if (existingPlayer != null)
                {
                    ReactivateExistingPlayer(existingPlayer, joinDateUtc);
                }
                else
                {
                    MATCH_PLAYER guestPlayer = CreateGuestPlayer(args, joinDateUtc);
                    dataBaseContext.MATCH_PLAYER.Add(guestPlayer);
                }

                dataBaseContext.SaveChanges();
                transaction.Commit();

                return JoinMatchResult.Success;
            }
        }

        private MATCH GetJoinableMatch(GuessWhoDBEntities dataBaseContext, JoinMatchArgs args)
        {
            MATCH match = dataBaseContext.MATCH
                .SingleOrDefault(m =>
                    m.MATCHID == args.MatchId &&
                    m.MATCHCODE == args.MatchCode);

            if (match == null)
            {
                return null;
            }

            if (!IsLobbyMatch(match))
            {
                return null;
            }

            return match;
        }

        private MATCH_PLAYER GetExistingPlayer(GuessWhoDBEntities dataBaseContext, JoinMatchArgs args)
        {
            return dataBaseContext.MATCH_PLAYER
                .SingleOrDefault(mp =>
                    mp.MATCHID == args.MatchId &&
                    mp.USERID == args.UserProfileId);
        }

        private bool IsExistingActivePlayer(MATCH_PLAYER existingPlayer)
        {
            return IsActivePlayer(existingPlayer);
        }

        private bool IsUserInActiveMatch(GuessWhoDBEntities dataBaseContext, long userProfileId, long currentMatchId)
        {
            return dataBaseContext.MATCH_PLAYER
                .Any(p =>
                    p.USERID == userProfileId &&
                    p.LEFTATUTC == null &&
                    p.MATCHID != currentMatchId);
        }

        private bool IsGuestSlotTaken(GuessWhoDBEntities dataBaseContext, long matchId, long userId)
        {
            return dataBaseContext.MATCH_PLAYER.Any(mp =>
                mp.MATCHID == matchId &&
                mp.SLOTNUMBER == GUEST_SLOT_NUMBER &&
                mp.LEFTATUTC == null &&
                mp.USERID != userId);
        }

        private void ReactivateExistingPlayer(MATCH_PLAYER existingPlayer, DateTime joinDateUtc)
        {
            existingPlayer.JOINEDATUTC = joinDateUtc;
            existingPlayer.LEFTATUTC = null;
            existingPlayer.ISREADY = false;
            existingPlayer.ISWINNER = false;
            existingPlayer.SLOTNUMBER = GUEST_SLOT_NUMBER;
        }

        private MATCH_PLAYER CreateGuestPlayer(JoinMatchArgs args, DateTime joinDateUtc)
        {
            return new MATCH_PLAYER
            {
                MATCHID = args.MatchId,
                USERID = args.UserProfileId,
                SLOTNUMBER = GUEST_SLOT_NUMBER,
                ISWINNER = false,
                JOINEDATUTC = joinDateUtc,
                LEFTATUTC = null,
                ISHOST = false,
                ISREADY = false
            };
        }

        public List<LobbyPlayerDto> GetMatchPlayers(long matchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return GetActivePlayersForMatch(dataBaseContext, matchId)
                    .OrderBy(p => p.SLOTNUMBER)
                    .Select(p => new LobbyPlayerDto
                    {
                        MatchId = p.MATCHID,
                        UserId = p.USERID,
                        DisplayName = dataBaseContext.USER_PROFILE
                            .Where(u => u.USERID == p.USERID)
                            .Select(u => u.DISPLAYNAME)
                            .FirstOrDefault(),
                        AvatarId = dataBaseContext.USER_PROFILE
                            .Where(u => u.USERID == p.USERID)
                            .Select(u => u.AVATARID)
                            .FirstOrDefault(),
                        SlotNumber = (byte)p.SLOTNUMBER,
                        IsReady = p.ISREADY,
                        IsHost = p.ISHOST
                    })
                    .ToList();
            }
        }

        public LeaveMatchResult LeaveMatch(MatchPlayerArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var match = dataBaseContext.MATCH
                    .SingleOrDefault(m => m.MATCHID == args.MatchId);

                if (match == null)
                {
                    return LeaveMatchResult.MatchNotFound;
                }

                var player = dataBaseContext.MATCH_PLAYER
                    .SingleOrDefault(mp =>
                        mp.MATCHID == args.MatchId &&
                        mp.USERID == args.UserProfileId);

                if (player == null)
                {
                    return LeaveMatchResult.PlayerNotInMatch;
                }

                if (player.LEFTATUTC != null)
                {
                    return LeaveMatchResult.PlayerAlreadyLeft;
                }

                var leftDateUtc = DateTime.UtcNow;

                MarkPlayerAsLeft(player, leftDateUtc);

                if (player.ISHOST)
                {
                    match.STATUSID = MATCH_STATUS_COMPLETED;

                    var remainingPlayers = GetActivePlayersForMatch(dataBaseContext, args.MatchId)
                        .ToList();

                    foreach (var other in remainingPlayers)
                    {
                        MarkPlayerAsLeft(other, leftDateUtc);
                    }
                }

                dataBaseContext.SaveChanges();
                transaction.Commit();

                return LeaveMatchResult.Success;
            }
        }

        public MarkReadyResult MarkPlayerAsReady(MatchPlayerArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var player = dataBaseContext.MATCH_PLAYER
                    .SingleOrDefault(mp =>
                        mp.MATCHID == args.MatchId &&
                        mp.USERID == args.UserProfileId);

                if (player == null)
                {
                    return MarkReadyResult.PlayerNotFound;
                }

                if (!IsActivePlayer(player))
                {
                    return MarkReadyResult.PlayerAlreadyLeft;
                }

                if (player.MATCH == null || !IsLobbyMatch(player.MATCH))
                {
                    return MarkReadyResult.MatchNotInLobby;
                }

                if (player.ISREADY)
                {
                    return MarkReadyResult.Success;
                }

                player.ISREADY = true;
                dataBaseContext.SaveChanges();

                return MarkReadyResult.Success;
            }
        }

        public bool ForceLeaveAllMatchesForUser(long userId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                DateTime nowUtc = DateTime.UtcNow;

                var activeEntries = dataBaseContext.MATCH_PLAYER
                    .Where(mp => mp.USERID == userId && mp.LEFTATUTC == null)
                    .ToList();

                if (activeEntries.Count == 0)
                {
                    return false;
                }

                foreach (var entry in activeEntries)
                {
                    MarkPlayerAsLeft(entry, nowUtc);
                }

                var hostMatchIds = activeEntries
                    .Where(e => e.ISHOST)
                    .Select(e => e.MATCHID)
                    .Distinct()
                    .ToList();

                if (hostMatchIds.Count > 0)
                {
                    var hostMatches = dataBaseContext.MATCH
                        .Where(m => hostMatchIds.Contains(m.MATCHID))
                        .ToList();

                    foreach (var match in hostMatches)
                    {
                        match.STATUSID = MATCH_STATUS_CANCELED;

                        var otherPlayers = GetActivePlayersForMatch(dataBaseContext, match.MATCHID)
                            .ToList();

                        foreach (var other in otherPlayers)
                        {
                            MarkPlayerAsLeft(other, nowUtc);
                        }
                    }
                }

                dataBaseContext.SaveChanges();
                transaction.Commit();

                return true;
            }
        }
    }
}
