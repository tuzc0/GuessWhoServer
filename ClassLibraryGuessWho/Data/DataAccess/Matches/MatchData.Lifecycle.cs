using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData : IMatchData
    {
        private readonly GuessWhoDBEntities dataContext;

        private const byte HOST_SLOT_NUMBER = 1;
        private const byte GUEST_SLOT_NUMBER = 2;
        private const int MIN_PLAYERS_TO_START = 2;
        private const byte MATCH_STATUS_LOBBY = 1;
        private const byte MATCH_STATUS_IN_PROGRESS = 2;
        private const byte MATCH_STATUS_COMPLETED = 3;
        private const byte MATCH_STATUS_CANCELED = 4;

        public MatchData(GuessWhoDBEntities context)
        {
            this.dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public MatchDto CreateMatchClassic(CreateMatchArgs args)
        {
            using (var transaction = dataContext.Database.BeginTransaction())
            {
                var match = new MATCH
                {
                    VISIBILITYID = args.Visibility,
                    STATUSID = args.MatchStatus,
                    MODEID = args.Mode,
                    MATCHCODE = args.MatchCode,
                    CREATEDATUTC = args.CreateDate
                };
                dataContext.MATCH.Add(match);
                dataContext.SaveChanges();

                var matchPlayer = new MATCH_PLAYER
                {
                    MATCHID = match.MATCHID,
                    USERID = args.UserProfileId,
                    SLOTNUMBER = HOST_SLOT_NUMBER,
                    ISHOST = true,
                    ISREADY = true,
                    JOINEDATUTC = args.CreateDate
                };
                dataContext.MATCH_PLAYER.Add(matchPlayer);
                dataContext.SaveChanges();
                transaction.Commit();
                return MapToDto(match);
            }
        }

        public StartMatchResult StartMatch(long matchId)
        {
            var match = dataContext.MATCH.SingleOrDefault(m => m.MATCHID == matchId);
            if (match == null) return StartMatchResult.MatchNotFound;
            if (!IsLobbyMatch(match)) return StartMatchResult.MatchNotInLobby;
            var activePlayers = GetActivePlayersForMatch(dataContext, matchId).ToList();
            if (activePlayers.Count < MIN_PLAYERS_TO_START) return StartMatchResult.NotEnoughPlayers;
            if (activePlayers.Any(p => !p.ISREADY)) return StartMatchResult.PlayersNotReady;

            match.STATUSID = MATCH_STATUS_IN_PROGRESS;
            match.STARTTIME = DateTime.UtcNow;
            dataContext.SaveChanges();
            return StartMatchResult.Success;
        }

        public EndMatchResult EndMatch(EndMatchArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            using (var transaction = dataContext.Database.BeginTransaction())
            {
                var match = dataContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId);
                if (match == null) return EndMatchResult.MatchNotFound;
                if (!IsInProgressMatch(match)) return EndMatchResult.MatchNotInProgress;

                var players = GetPlayersForMatch(dataContext, args.MatchId).ToList();
                var winner = players.SingleOrDefault(p => p.USERID == args.WinnerUserId);
                if (winner == null) return EndMatchResult.WinnerNotInMatch;

                FinalizeMatchWithWinner(match, players, winner, DateTime.UtcNow);
                dataContext.SaveChanges();
                transaction.Commit();
                return EndMatchResult.Success;
            }
        }

        public MatchDto GetOpenMatchByCode(string matchCode)
        {
            var match = dataContext.MATCH.AsNoTracking().SingleOrDefault(m =>
                m.MATCHCODE == matchCode && m.STARTTIME == null && m.ENDTIME == null);
            return match == null ? MatchDto.CreateInvalid() : MapToDto(match);
        }

        private MatchDto MapToDto(MATCH match)
        {
            if (match == null) return MatchDto.CreateInvalid();
            return new MatchDto
            {
                MatchId = match.MATCHID,
                Code = match.MATCHCODE,
                StatusId = match.STATUSID,
                Mode = match.MODEID,
                VisibilityId = match.VISIBILITYID,
                CreateAtUtc = match.CREATEDATUTC
            };
        }
    }
}