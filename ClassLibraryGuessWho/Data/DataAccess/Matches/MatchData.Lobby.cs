using GuessWhoCore.Contracts.Response;
using GuessWhoCore.Dtos;
using GuessWhoCore.Enums;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData
    {
        public JoinMatchResult AddPlayerToMatchByCode(JoinMatchArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.MatchId <= 0 || args.UserProfileId <= 0 || string.IsNullOrWhiteSpace(args.MatchCode))
            {
                return JoinMatchResult.MatchNotJoinable;
            }

            bool isInOtherActiveMatch = _dataContext.MATCH_PLAYER.Any(mp =>
                mp.USERID == args.UserProfileId &&
                mp.LEFTATUTC == null &&
                mp.MATCHID != args.MatchId &&
                mp.MATCH.STATUSID != MatchStatusIds.FINISHED &&
                mp.MATCH.STATUSID != MatchStatusIds.CANCELLED);

            if (isInOtherActiveMatch)
            {
                return JoinMatchResult.InOtherActiveMatch;
            }

            MATCH match = _dataContext.MATCH.SingleOrDefault(m =>
                m.MATCHID == args.MatchId &&
                m.MATCHCODE == args.MatchCode);

            if (match == null)
            {
                return JoinMatchResult.MatchNotFound;
            }

            if (!IsLobbyMatch(match) || match.STARTTIME != null || match.ENDTIME != null)
            {
                return JoinMatchResult.MatchNotJoinable;
            }

            if (match.ISCODEJOINENABLED == false)
            {
                return JoinMatchResult.MatchNotJoinable;
            }

            var activePlayers = GetActivePlayersForMatch(_dataContext, args.MatchId).ToList();

            if (activePlayers.Count >= MAX_MATCH_PLAYERS_BY_SCHEMA)
            {
                return JoinMatchResult.GuestSlotTaken;
            }

            if (HasDuplicateActiveSlot(activePlayers))
            {
                return JoinMatchResult.MatchNotJoinable;
            }

            MATCH_PLAYER existing = _dataContext.MATCH_PLAYER.SingleOrDefault(mp =>
                mp.MATCHID == args.MatchId &&
                mp.USERID == args.UserProfileId);

            if (IsActivePlayer(existing))
            {
                return JoinMatchResult.PlayerAlreadyInMatch;
            }

            try
            {
                bool isRejoin = existing != null;

                if (isRejoin)
                {
                    if (activePlayers.Any(p => p != null && p.SLOTNUMBER == existing.SLOTNUMBER))
                    {
                        return JoinMatchResult.MatchNotJoinable;
                    }

                    existing.LEFTATUTC = null;
                    existing.ISREADY = false;
                    existing.JOINEDATUTC = DateTime.UtcNow;
                }
                else
                {
                    bool hasHost = activePlayers.Any(p => p != null && p.SLOTNUMBER == HOST_SLOT_NUMBER);
                    byte slotToUse = hasHost ? GUEST_SLOT_NUMBER : HOST_SLOT_NUMBER;

                    if (activePlayers.Any(p => p != null && p.SLOTNUMBER == slotToUse))
                    {
                        return JoinMatchResult.GuestSlotTaken;
                    }

                    _dataContext.MATCH_PLAYER.Add(new MATCH_PLAYER
                    {
                        MATCHID = args.MatchId,
                        USERID = args.UserProfileId,
                        SLOTNUMBER = slotToUse,
                        ISHOST = slotToUse == HOST_SLOT_NUMBER,
                        ISREADY = false,
                        JOINEDATUTC = DateTime.UtcNow
                    });
                }

                _dataContext.SaveChanges();

                return JoinMatchResult.Success;
            }
            catch (DbUpdateException ex)
            {
                return JoinMatchResult.MatchNotJoinable;
            }
        }

        public LeaveMatchResult LeaveMatch(MatchPlayerArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.MatchId <= 0 || args.UserProfileId <= 0)
            {
                return LeaveMatchResult.PlayerNotInMatch;
            }

            MATCH match = _dataContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId);
            if (match == null)
            {
                return LeaveMatchResult.MatchNotFound;
            }

            MATCH_PLAYER player = _dataContext.MATCH_PLAYER.SingleOrDefault(mp =>
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

            DateTime utcNow = DateTime.UtcNow;

            MarkPlayerAsLeft(player, utcNow);

            if (player.ISHOST && !IsMatchCompleted(match))
            {
                match.STATUSID = MatchStatusIds.CANCELLED;
                match.ENDTIME = utcNow;

                foreach (MATCH_PLAYER active in GetActivePlayersForMatch(_dataContext, args.MatchId).ToList())
                {
                    MarkPlayerAsLeft(active, utcNow);
                }
            }

            _dataContext.SaveChanges();

            return LeaveMatchResult.Success;
        }

        public MarkReadyResult MarkPlayerAsReady(MatchPlayerArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.MatchId <= 0 || args.UserProfileId <= 0)
            {
                return MarkReadyResult.PlayerNotFound;
            }

            MATCH match = _dataContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId);
            if (match == null)
            {
                return MarkReadyResult.PlayerNotFound;
            }

            if (!IsLobbyMatch(match))
            {
                return MarkReadyResult.MatchNotInLobby;
            }

            MATCH_PLAYER player = _dataContext.MATCH_PLAYER.SingleOrDefault(mp =>
                mp.MATCHID == args.MatchId &&
                mp.USERID == args.UserProfileId);

            if (player == null)
            {
                return MarkReadyResult.PlayerNotFound;
            }

            if (player.LEFTATUTC != null)
            {
                return MarkReadyResult.PlayerAlreadyLeft;
            }

            player.ISREADY = true;

            _dataContext.SaveChanges();

            return MarkReadyResult.Success;
        }

        public ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.MatchId <= 0 || args.UserProfileId <= 0)
            {
                return ChooseSecretCharacterResult.MatchNotFound;
            }

            if (string.IsNullOrWhiteSpace(args.SecretCharacterId))
            {
                return ChooseSecretCharacterResult.InvalidCharacter;
            }

            MATCH match = _dataContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId);
            if (match == null)
            {
                return ChooseSecretCharacterResult.MatchNotFound;
            }

            if (!IsActiveMatch(match))
            {
                return ChooseSecretCharacterResult.MatchNotInProgress;
            }

            MATCH_PLAYER player = _dataContext.MATCH_PLAYER.SingleOrDefault(mp =>
                mp.MATCHID == args.MatchId &&
                mp.USERID == args.UserProfileId);

            if (player == null)
            {
                return ChooseSecretCharacterResult.PlayerNotInMatch;
            }

            if (player.LEFTATUTC != null)
            {
                return ChooseSecretCharacterResult.PlayerAlreadyLeft;
            }

            if (!string.IsNullOrWhiteSpace(player.SECRETCHARACTERID))
            {
                return ChooseSecretCharacterResult.SecretAlreadyChosen;
            }

            bool characterExists = _dataContext.CHARACTER.Any(c =>
                c.CHARACTERID == args.SecretCharacterId &&
                c.ISACTIVE);

            if (!characterExists)
            {
                return ChooseSecretCharacterResult.InvalidCharacter;
            }

            player.SECRETCHARACTERID = args.SecretCharacterId;

            _dataContext.SaveChanges();

            return ChooseSecretCharacterResult.Success;
        }

        public bool AreAllSecretCharactersChosen(long matchId)
        {
            if (matchId <= 0)
            {
                return false;
            }

            var players = GetActivePlayersForMatch(_dataContext, matchId).ToList();

            return players.Any() &&
                   players.All(p => p != null && !string.IsNullOrWhiteSpace(p.SECRETCHARACTERID));
        }

        public bool ForceLeaveAllMatchesForUser(long userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            DateTime utcNow = DateTime.UtcNow;

            var entries = _dataContext.MATCH_PLAYER
                .Where(mp => mp.USERID == userId && mp.LEFTATUTC == null)
                .ToList();

            if (!entries.Any())
            {
                return false;
            }

            foreach (MATCH_PLAYER entry in entries)
            {
                MarkPlayerAsLeft(entry, utcNow);
            }

            _dataContext.SaveChanges();

            return true;
        }

        public List<LobbyPlayerDto> GetMatchPlayers(long matchId)
        {
            if (matchId <= 0)
            {
                return new List<LobbyPlayerDto>();
            }

            return (from mp in _dataContext.MATCH_PLAYER
                    join up in _dataContext.USER_PROFILE on mp.USERID equals up.USERID
                    where mp.MATCHID == matchId && mp.LEFTATUTC == null
                    select new LobbyPlayerDto
                    {
                        MatchId = mp.MATCHID,
                        UserId = mp.USERID,
                        DisplayName = up.DISPLAYNAME ?? string.Empty,
                        AvatarId = up.AVATARID ?? string.Empty,
                        SlotNumber = mp.SLOTNUMBER,
                        IsReady = mp.ISREADY,
                        IsHost = mp.ISHOST
                    })
                .ToList();
        }
    }
}
