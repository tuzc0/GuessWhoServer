using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Leaderboard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Leaderboard
{
    public sealed class LeaderboardData : ILeaderboardRepository
    {
        private const string EMPTY = "";
        private const string DEFAULT_DISPLAY_NAME = "Unknown";
        private const string DEFAULT_AVATAR = "A0001";

        private readonly GuessWhoDBEntities dataContext;

        public LeaderboardData(GuessWhoDBEntities context)
        {
            dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IList<LeaderboardPlayer> GetTopWinners(int limit)
        {
            var query = dataContext.MATCH_PLAYER
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
                .Take(limit)
                .ToList();

            return query.Select((item, index) => new LeaderboardPlayer
            {
                Rank = index + 1,
                DisplayName = item.DisplayName ?? DEFAULT_DISPLAY_NAME,
                AvatarId = item.AvatarId ?? DEFAULT_AVATAR,
                Wins = item.Wins
            }).ToList();
        }

        public LeaderboardPlayer GetPlayerStats(long userId)
        {
            var userStats = dataContext.MATCH_PLAYER
                .Where(mp => mp.USERID == userId && mp.ISWINNER == true)
                .GroupBy(mp => mp.USERID)
                .Select(g => new { Wins = g.Count() })
                .FirstOrDefault();

            int myWins = userStats != null ? userStats.Wins : 0;

            USER_PROFILE profileEntity = FindUserProfile(userId);

            if (profileEntity == null)
            {
                return null;
            }

            int rank = CalculateUserRank(myWins);

            return new LeaderboardPlayer
            {
                Rank = rank,
                DisplayName = profileEntity.DISPLAYNAME ?? EMPTY,
                AvatarId = profileEntity.AVATARID ?? DEFAULT_AVATAR,
                Wins = myWins
            };
        }

        private USER_PROFILE FindUserProfile(long userId)
        {
            return dataContext.USER_PROFILE
                .AsNoTracking()
                .SingleOrDefault(p => p.USERID == userId);
        }

        private int CalculateUserRank(int myWins)
        {
            return dataContext.MATCH_PLAYER
                .Where(mp => mp.ISWINNER == true)
                .GroupBy(mp => mp.USERID)
                .Select(g => g.Count())
                .Count(w => w > myWins) + 1;
        }
    }
}