using ClassLibraryGuessWho.Contracts.Dtos;
using ClassLibraryGuessWho.Contracts.DTOs.DTO;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public class MatchData
    {
        public MatchDto CreateMatch(CreateMatchArgs args)
        {

            using (var dataBaseContext = new GuessWhoDB())
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
                    SLOTNUMBER = 1,
                    ISWINNER = false,
                    JOINEDATUTC = args.CreateDate,
                    LEFTATUTC = null,
                    ISHOST = true,
                    ISREADY = false
                };

                var matchDto = new MatchDto
                {
                    MatchId = match.MATCHID,
                    Code = match.MATCHCODE,
                    StatusId = match.STATUSID,
                    Mode = match.MODE,
                    VisibilityId = match.VISIBILITYID,
                    CreateAtUtc = match.CREATEDATUTC,
                };

                dataBaseContext.MATCH_PLAYER.Add(matchPlayer);
                dataBaseContext.SaveChanges();
                transaction.Commit();
                return matchDto;
            }
        }

        public MATCH GetMatchByCode(JoinMatchArgs args)
        {
            using (var dataBaseContext = new GuessWhoDB())
            using (var transaction = dataBaseContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                var matchId = dataBaseContext.MATCH
                    .AsNoTracking()
                    .Where(m => m.MATCHCODE == args.MatchCode)
                    .Select(m => (long?)m.MATCHID)
                    .SingleOrDefault();

                if (matchId == null)
                {
                    return null;
                }

                bool isAlreadyPlayer = dataBaseContext.MATCH_PLAYER
                    .Any(p => p.MATCHID == matchId.Value && p.USERID == args.UserProfileId && p.LEFTATUTC == null);

                if (isAlreadyPlayer)
                {
                    return dataBaseContext.MATCH
                        .AsNoTracking()
                        .Single(m => m.MATCHID == matchId);
                }

                var FreeSlotNumber = !dataBaseContext.MATCH_PLAYER
                    .Any(p => p.MATCHID == matchId && p.LEFTATUTC == null && p.SLOTNUMBER == 2);

                if (!FreeSlotNumber)
                {
                    throw new FaultException("The math is full");
                }

                dataBaseContext.MATCH_PLAYER.Add(new MATCH_PLAYER
                {
                    MATCHID = matchId.Value,
                    USERID = args.UserProfileId,
                    SLOTNUMBER = 2,
                    ISWINNER = false,
                    JOINEDATUTC = args.JoinedDate,
                    LEFTATUTC = null,
                    ISHOST = false,
                    ISREADY = false
                });

                try
                {
                    dataBaseContext.SaveChanges();
                    transaction.Commit();
                }
                catch (DbUpdateException)
                {
                    throw new FaultException("Could not join the match: The match is full");
                }

                return dataBaseContext.MATCH
                    .AsNoTracking()
                    .Single(m => m.MATCHID == matchId);
            }
        }

        public List<LobbyPlayerDto> GetMatchPlayers(long matchId)
        {
            using (var dataBaseContext = new GuessWhoDB())
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
    }
}