using ClassLibraryGuessWho.Data;
using GuessWhoContracts.Dtos.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Leaderboard
{
    public sealed class LeaderboardData
    {
        public List<LeaderboardPlayerDto> GetTopWinners(int topN)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var query = dataBaseContext.MATCH_PLAYER
                    .Where(mp => mp.ISWINNER == true)
                    .GroupBy(mp => new
                    {
                        mp.USERID,
                        mp.USER_PROFILE.DISPLAYNAME,
                        mp.USER_PROFILE.AVATARID
                    })
                    .Select(g => new
                    {
                        DisplayName = g.Key.DISPLAYNAME,
                        AvatarId = g.Key.AVATARID,
                        Wins = g.Count()
                    })
                    .OrderByDescending(x => x.Wins)
                    .Take(topN)
                    .ToList();

                var leaderboardList = new List<LeaderboardPlayerDto>();
                int rankCounter = 1;

                foreach (var item in query)
                {
                    leaderboardList.Add(new LeaderboardPlayerDto
                    {
                        Rank = rankCounter,
                        DisplayName = item.DisplayName,
                        AvatarId = item.AvatarId,
                        Wins = item.Wins
                    });

                    rankCounter++;
                }

                return leaderboardList;
            }
        }

        public LeaderboardPlayerDto GetPlayerStats(int userId)
        {
            using (var context = new GuessWhoDBEntities())
            {
                var userStats = context.MATCH_PLAYER
                    .Where(mp => mp.USERID == userId && mp.ISWINNER == true)
                    .GroupBy(mp => mp.USERID)
                    .Select(g => new
                    {
                        Wins = g.Count()
                    })
                    .FirstOrDefault();

                int myWins = userStats != null ? userStats.Wins : 0;

                var profile = context.USER_PROFILE
                    .AsNoTracking()
                    .FirstOrDefault(u => u.USERID == userId);

                if (profile == null) return null;

                var rank = context.MATCH_PLAYER
                    .Where(mp => mp.ISWINNER == true)
                    .GroupBy(mp => mp.USERID)
                    .Select(g => g.Count())
                    .Count(w => w > myWins) + 1;

                return new LeaderboardPlayerDto
                {
                    Rank = rank,
                    DisplayName = profile.DISPLAYNAME,
                    AvatarId = profile.AVATARID,
                    Wins = myWins
                };
            }
        }
    }
}