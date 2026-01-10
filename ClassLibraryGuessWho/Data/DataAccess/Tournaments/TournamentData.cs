using GuessWhoServerDomain.Domain.Interfaces.Repositories;
using GuessWhoServerDomain.Domain.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuessWhoDataAccess.Data.DataAccess.Tournaments
{
    public sealed class TournamentData : ITournamentRepository
    {
        private readonly GuessWhoDBEntities dataContext;
        private const long INVALID_ID = 0;

        public TournamentData(GuessWhoDBEntities context)
        {
            this.dataContext = context;
        }

        public long CreateTournament(long hostUserId, int turnSeconds)
        {
            var entity = new TOURNAMENT_4P
            {
                HOSTUSERID = hostUserId,
                TURNSECONDS = turnSeconds,
                STATUSID = (byte)1,
                CREATEDATUTC = DateTime.UtcNow
            };

            dataContext.TOURNAMENT_4P.Add(entity);
            dataContext.SaveChanges();

            return (long)entity.TOURNAMENTID;
        }

        public bool AddPlayerToTournament(long tournamentId, long userId)
        {
            var entity = new TOURNAMENT_4P_PLAYER
            {
                TOURNAMENTID = (int)tournamentId,
                USERID = userId,
                SLOTNUMBER = (byte)0
            };

            dataContext.TOURNAMENT_4P_PLAYER.Add(entity);
            return dataContext.SaveChanges() > 0;
        }

        public IEnumerable<TournamentPlayer> GetTournamentPlayers(long tournamentId)
        {
            return dataContext.TOURNAMENT_4P_PLAYER
                .Where(p => p.TOURNAMENTID == (int)tournamentId)
                .Select(p => new TournamentPlayer
                {
                    TournamentId = (long)p.TOURNAMENTID,
                    UserId = p.USERID,
                    JoinedAtUtc = DateTime.UtcNow,
                    DisplayName = p.USER_PROFILE.DISPLAYNAME
                }).ToList();
        }

        public int GetPlayerCount(long tournamentId)
        {
            return dataContext.TOURNAMENT_4P_PLAYER.Count(p => p.TOURNAMENTID == (int)tournamentId);
        }

        public void LinkMatchToTournament(long tournamentId, long matchId, bool isFinal)
        {
            var entity = new TOURNAMENT_4P_MATCH
            {
                TOURNAMENTID = (int)tournamentId,
                MATCHID = matchId,
                ROUNDNUMBER = (byte)(isFinal ? 2 : 1),
                BRACKETPOSITION = (byte)0
            };

            dataContext.TOURNAMENT_4P_MATCH.Add(entity);
        }

        public void UpdateStatus(long tournamentId, int statusId)
        {
            var tournament = dataContext.TOURNAMENT_4P.FirstOrDefault(t => t.TOURNAMENTID == (int)tournamentId);
            if (tournament != null)
            {
                tournament.STATUSID = (byte)statusId;
            }
        }

        public bool HandleDisconnect(long userId, DateTime nowUtc)
        {
            if (userId <= INVALID_ID)
            {
                return false;
            }

            try
            {
                var waitingPlayers = dataContext.TOURNAMENT_4P_PLAYER
                    .Where(p => p.USERID == userId && p.TOURNAMENT_4P.STATUSID == 1)
                    .ToList();

                if (waitingPlayers.Any())
                {
                    dataContext.TOURNAMENT_4P_PLAYER.RemoveRange(waitingPlayers);
                    return dataContext.SaveChanges() > 0;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}