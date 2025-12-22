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
            using (var transaction = dataContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                MATCH match = dataContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId && m.MATCHCODE == args.MatchCode);
                if (match == null || !IsLobbyMatch(match)) return JoinMatchResult.MatchNotJoinable;

                MATCH_PLAYER existing = dataContext.MATCH_PLAYER.SingleOrDefault(mp => mp.MATCHID == args.MatchId && mp.USERID == args.UserProfileId);
                if (IsActivePlayer(existing)) return JoinMatchResult.PlayerAlreadyInMatch;

                if (existing != null)
                {
                    existing.LEFTATUTC = null;
                    existing.ISREADY = false;
                }
                else
                {
                    dataContext.MATCH_PLAYER.Add(new MATCH_PLAYER
                    {
                        MATCHID = args.MatchId,
                        USERID = args.UserProfileId,
                        SLOTNUMBER = GUEST_SLOT_NUMBER,
                        JOINEDATUTC = DateTime.UtcNow
                    });
                }
                dataContext.SaveChanges();
                transaction.Commit();
                return JoinMatchResult.Success;
            }
        }

        public LeaveMatchResult LeaveMatch(MatchPlayerArgs args)
        {
            using (var transaction = dataContext.Database.BeginTransaction())
            {
                var match = dataContext.MATCH.Find(args.MatchId);
                var player = dataContext.MATCH_PLAYER.SingleOrDefault(mp => mp.MATCHID == args.MatchId && mp.USERID == args.UserProfileId);
                if (player == null) return LeaveMatchResult.PlayerNotInMatch;

                DateTime now = DateTime.UtcNow;
                MarkPlayerAsLeft(player, now);
                if (player.ISHOST)
                {
                    match.STATUSID = MATCH_STATUS_COMPLETED;
                    foreach (var p in GetActivePlayersForMatch(dataContext, args.MatchId)) MarkPlayerAsLeft(p, now);
                }
                dataContext.SaveChanges();
                transaction.Commit();
                return LeaveMatchResult.Success;
            }
        }

        public MarkReadyResult MarkPlayerAsReady(MatchPlayerArgs args)
        {
            var player = dataContext.MATCH_PLAYER.SingleOrDefault(mp => mp.MATCHID == args.MatchId && mp.USERID == args.UserProfileId);
            if (player == null) return MarkReadyResult.PlayerNotFound;
            player.ISREADY = true;
            dataContext.SaveChanges();
            return MarkReadyResult.Success;
        }

        public ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs args)
        {
            var player = dataContext.MATCH_PLAYER.SingleOrDefault(mp => mp.MATCHID == args.MatchId && mp.USERID == args.UserProfileId);
            if (player == null) return ChooseSecretCharacterResult.PlayerNotInMatch;
            player.SECRETCHARACTERID = args.SecretCharacterId;
            dataContext.SaveChanges();
            return ChooseSecretCharacterResult.Success;
        }

        public bool AreAllSecretCharactersChosen(long matchId)
        {
            var players = GetActivePlayersForMatch(dataContext, matchId).ToList();
            return players.Any() && players.All(p => !string.IsNullOrEmpty(p.SECRETCHARACTERID));
        }

        public bool ForceLeaveAllMatchesForUser(long userId)
        {
            using (var transaction = dataContext.Database.BeginTransaction())
            {
                var entries = dataContext.MATCH_PLAYER.Where(mp => mp.USERID == userId && mp.LEFTATUTC == null).ToList();
                if (!entries.Any()) return false;
                foreach (var e in entries) MarkPlayerAsLeft(e, DateTime.UtcNow);
                dataContext.SaveChanges();
                transaction.Commit();
                return true;
            }
        }

        public List<LobbyPlayerDto> GetMatchPlayers(long matchId)
        {
            return GetActivePlayersForMatch(dataContext, matchId).Select(p => new LobbyPlayerDto
            {
                UserId = p.USERID,
                IsReady = p.ISREADY,
                IsHost = p.ISHOST
            }).ToList();
        }
    }
}