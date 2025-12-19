using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Enums;
using System;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData
    {
        private const byte HOST_SLOT_NUMBER = 1;
        private const byte GUEST_SLOT_NUMBER = 2;
        private const int MIN_PLAYERS_TO_START = 2;
        private const byte MATCH_STATUS_LOBBY = 1;
        private const byte MATCH_STATUS_IN_PROGRESS = 2;
        private const byte MATCH_STATUS_COMPLETED = 3;
        private const byte MATCH_STATUS_CANCELED = 4;

        public MatchDto CreateMatchClassic(CreateMatchArgs args)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            using (var transaction = dataBaseContext.Database.BeginTransaction())
            {
                var match = new MATCH
                {
                    VISIBILITYID = args.Visibility,
                    STATUSID = args.MatchStatus,
                    MODEID = args.Mode,
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
                    ISREADY = true
                };

                dataBaseContext.MATCH_PLAYER.Add(matchPlayer);
                dataBaseContext.SaveChanges();
                transaction.Commit();

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

        public StartMatchResult StartMatch(long matchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var match = dataBaseContext.MATCH
                    .SingleOrDefault(m => m.MATCHID == matchId);

                if (match == null)
                {
                    return StartMatchResult.MatchNotFound;
                }

                if (!IsLobbyMatch(match))
                {
                    return StartMatchResult.MatchNotInLobby;
                }

                var activePlayers = GetActivePlayersForMatch(dataBaseContext, matchId)
                    .ToList();

                if (activePlayers.Count < MIN_PLAYERS_TO_START)
                {
                    return StartMatchResult.NotEnoughPlayers;
                }

                if (activePlayers.Any(p => !p.ISREADY))
                {
                    return StartMatchResult.PlayersNotReady;
                }

                match.STATUSID = MATCH_STATUS_IN_PROGRESS;
                match.STARTTIME = DateTime.UtcNow;

                dataBaseContext.SaveChanges();

                return StartMatchResult.Success;
            }
        }

        public EndMatchResult EndMatch(EndMatchArgs args)
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
                    return EndMatchResult.MatchNotFound;
                }

                if (!IsInProgressMatch(match))
                {
                    return EndMatchResult.MatchNotInProgress;
                }

                var players = GetPlayersForMatch(dataBaseContext, args.MatchId)
                    .ToList();

                var winnerPlayer = players
                    .SingleOrDefault(p => p.USERID == args.WinnerUserId);

                if (winnerPlayer == null)
                {
                    return EndMatchResult.WinnerNotInMatch;
                }

                DateTime nowUtc = DateTime.UtcNow;

                FinalizeMatchWithWinner(
                    match,
                    players,
                    winnerPlayer,
                    nowUtc);

                dataBaseContext.SaveChanges();
                transaction.Commit();

                return EndMatchResult.Success;
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
                Mode = match.MODEID,
                VisibilityId = match.VISIBILITYID,
                CreateAtUtc = match.CREATEDATUTC
            };
        }

        public ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (string.IsNullOrWhiteSpace(args.SecretCharacterId))
            {
                return ChooseSecretCharacterResult.InvalidCharacter;
            }

            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var match = dataBaseContext.MATCH
                    .SingleOrDefault(m => m.MATCHID == args.MatchId);

                if (match == null)
                {
                    return ChooseSecretCharacterResult.MatchNotFound;
                }

                if (match.STATUSID != MATCH_STATUS_IN_PROGRESS)
                {
                    return ChooseSecretCharacterResult.MatchNotInProgress;
                }

                var player = dataBaseContext.MATCH_PLAYER
                    .SingleOrDefault(mp =>
                        mp.MATCHID == args.MatchId &&
                        mp.USERID == args.UserProfileId);

                if (player == null)
                {
                    return ChooseSecretCharacterResult.PlayerNotInMatch;
                }

                if (!IsActivePlayer(player))
                {
                    return ChooseSecretCharacterResult.PlayerAlreadyLeft;
                }

                if (!string.IsNullOrEmpty(player.SECRETCHARACTERID))
                {
                    return ChooseSecretCharacterResult.SecretAlreadyChosen;
                }

                player.SECRETCHARACTERID = args.SecretCharacterId;

                dataBaseContext.SaveChanges();

                return ChooseSecretCharacterResult.Success;
            }
        }

        public bool AreAllSecretCharactersChosen(long matchId)
        {
            using (var dataBaseContext = new GuessWhoDBEntities())
            {
                var activePlayers = GetActivePlayersForMatch(dataBaseContext, matchId)
                    .ToList();

                if (activePlayers.Count == 0)
                {
                    return false;
                }

                return activePlayers.All(p => !string.IsNullOrEmpty(p.SECRETCHARACTERID));
            }
        }
    }
}
