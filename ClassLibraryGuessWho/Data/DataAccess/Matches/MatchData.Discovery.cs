using GuessWhoServerDomain.Domain.Models.Match;
using GuessWhoServerDomain.Domain.Models.Matches;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        public MatchSnapshot GetOpenMatchByCode(string matchCode)
        {
            string safeCode = matchCode ?? string.Empty;

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

        public IReadOnlyList<LobbyPlayerSnapshot> GetMatchPlayers(long matchId)
        {
            if (matchId <= 0)
            {
                return Array.Empty<LobbyPlayerSnapshot>();
            }

            List<LobbyPlayerSnapshot> players = (from matchPlayerEntity in dataContext.MATCH_PLAYER.AsNoTracking()
                                                 join userProfileEntity in dataContext.USER_PROFILE.AsNoTracking()
                                                 on matchPlayerEntity.USERID equals userProfileEntity.USERID
                                                 where matchPlayerEntity.MATCHID == matchId && matchPlayerEntity.LEFTATUTC == null
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
    }
}
