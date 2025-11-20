using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed class MatchData
    {
        private const byte HOST_SLOT_NUMBER = 1;
        private const byte GUEST_SLOT_NUMBER = 2;
        private const byte MAX_ACTIVE_PLAYERS_PER_MATCH = 2;
        private const byte MATCH_STATUS_COMPLETED = 3;

        public MatchDto CreateMatchClassic(CreateMatchArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var match = new MATCH
                {
                    VISIBILITYID = args.Visibility,
                    STATUSID = args.MatchStatus,
                    MODE = args.Mode,
                    MATCHCODE = args.MatchCode,
                    CREATEDATUTC = args.CreateDate,
                    STARTTIME = null,
                    ENDTIME = null,
                    WINNERUSERID = null
                };

                dataBaseContext.MATCH.Add(match);
                dataBaseContext.SaveChanges();

                var matchPlayer = new MATCH_PLAYER
                {
                    MATCHID = match.MATCHID,
                    USERID = args.UserProfileId,
                    SLOTNUMBER = HOST_SLOT_NUMBER,
                    ISWINNER = false,
                    JOINEDATUTC = args.CreateDate,
                    LEFTATUTC = null,
                    ISHOST = true,
                    ISREADY = false
                };

                dataBaseContext.MATCH_PLAYER.Add(matchPlayer);
                dataBaseContext.SaveChanges();
                transaction.Commit();

                return new MatchDto
                {
                    MatchId = match.MATCHID,
                    Code = match.MATCHCODE,
                    StatusId = match.STATUSID,
                    Mode = match.MODE,
                    VisibilityId = match.VISIBILITYID,
                    CreateAtUtc = match.CREATEDATUTC
                };
            }
        }

        public bool IsUserInMatch(long userProfileId, long matchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.MATCH_PLAYER
                    .Any(p =>
                    p.MATCHID == matchId &&
                    p.USERID == userProfileId &&
                    p.LEFTATUTC == null);
            }
        }   

        public bool IsUserInActiveMatch(long userProfileId, long currentMatchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.MATCH_PLAYER
                    .Any(p =>
                    p.USERID == userProfileId &&
                    p.LEFTATUTC == null &&
                    p.MATCHID != currentMatchId);
            }
        }

        public MatchDto GetOpenMatchByCode(string matchCode)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var match = dataBaseContext.MATCH
                    .AsNoTracking()
                    .SingleOrDefault(m => 
                    m.MATCHCODE == matchCode &&
                    m.STARTTIME == null &&
                    m.ENDTIME == null);

                if (match == null)
                {
                    return MatchDto.CreateInvalid();
                }

                return MapToDto(match);
            }
        }

        public bool HasAvailableSlotInMatch(long matchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                int activePlayers = dataBaseContext.MATCH_PLAYER
                    .Count(p => p.MATCHID == matchId && p.LEFTATUTC == null);

                return activePlayers < MAX_ACTIVE_PLAYERS_PER_MATCH;
            }
        }

        public bool AddPlayerToMatchByCode(JoinMatchArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var existingPlayer = dataBaseContext.MATCH_PLAYER
                    .SingleOrDefault(mp =>
                        mp.MATCHID == args.MatchId &&
                        mp.USERID == args.UserProfileId);

                if (existingPlayer != null && existingPlayer.LEFTATUTC == null)
                {
                    return false;
                }

                if (existingPlayer != null && existingPlayer.LEFTATUTC != null)
                {
                    existingPlayer.JOINEDATUTC = args.JoinedDate;
                    existingPlayer.LEFTATUTC = null;
                    existingPlayer.ISREADY = false;
                    existingPlayer.ISWINNER = false;
                    existingPlayer.SLOTNUMBER = GUEST_SLOT_NUMBER;

                    dataBaseContext.SaveChanges();
                    transaction.Commit();
                    return true;
                }

                var guestPlayer = new MATCH_PLAYER
                {
                    MATCHID = args.MatchId,
                    USERID = args.UserProfileId,
                    SLOTNUMBER = GUEST_SLOT_NUMBER,
                    ISWINNER = false,
                    JOINEDATUTC = args.JoinedDate,
                    LEFTATUTC = null,
                    ISHOST = false,
                    ISREADY = false
                };

                dataBaseContext.MATCH_PLAYER.Add(guestPlayer);
                dataBaseContext.SaveChanges();
                transaction.Commit();

                return true;
            }
        }


        public List<LobbyPlayerDto> GetMatchPlayers(long matchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                return dataBaseContext.MATCH_PLAYER
                    .AsNoTracking()
                    .Where(mp => mp.MATCHID == matchId && mp.LEFTATUTC == null)
                    .OrderBy(p => p.SLOTNUMBER)
                    .Select(p => new LobbyPlayerDto
                    {
                        MatchId = p.MATCHID,
                        UserId = p.USERID,
                        DisplayName = dataBaseContext.USER_PROFILE
                            .Where(u => u.USERID == p.USERID)
                            .Select(u => u.DISPLAYNAME)
                            .FirstOrDefault(),
                        SlotNumber = (byte)p.SLOTNUMBER,
                        IsReady = p.ISREADY,
                        IsHost = p.ISHOST
                    })
                    .ToList();
            }
        }

        public LeaveMatchResult LeaveMatch(LeaveMatchArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var match = dataBaseContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId);

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

                var leftDate = args.LeftDate;

                player.LEFTATUTC = leftDate;

                if (player.ISHOST)
                {
                    match.STATUSID = MATCH_STATUS_COMPLETED;

                    var remainingPlayers = dataBaseContext.MATCH_PLAYER
                        .Where(mp => mp.MATCHID == args.MatchId && mp.LEFTATUTC == null)
                        .ToList();

                    foreach (var other in remainingPlayers)
                    {
                        other.LEFTATUTC = leftDate;
                    }
                }

                dataBaseContext.SaveChanges();
                transaction.Commit();
                return LeaveMatchResult.Success;
            }
        }

        private static MatchDto MapToDto(MATCH match)
        {
            if (match == null)
            {
                return MatchDto.CreateInvalid();
            }

            return new MatchDto
            {
                MatchId = match.MATCHID,
                Code = match.MATCHCODE,
                StatusId = match.STATUSID,
                Mode = match.MODE,
                VisibilityId = match.VISIBILITYID,
                CreateAtUtc = match.CREATEDATUTC
            };
        }
    }
}
