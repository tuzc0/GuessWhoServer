using GuessWhoServerDomain.Domain.Parameters.Matches;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace ClassLibraryGuessWho.Data.DataAccess.Match
{
    public sealed partial class MatchData : IMatchData
    {
        private readonly GuessWhoDBEntities _dataContext;

        private const int MATCH_CODE_LENGTH = 10;

        private const string MATCH_CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public MatchData(GuessWhoDBEntities context)
        {
            _dataContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public MatchDto CreateMatchClassic(CreateMatchArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.UserProfileId <= 0 || string.IsNullOrWhiteSpace(args.MatchCode))
            {
                return MatchDto.CreateInvalid();
            }

            var match = new MATCH
            {
                VISIBILITYID = args.Visibility,
                STATUSID = args.MatchStatus,
                MODEID = args.Mode,
                MATCHCODE = args.MatchCode,
                CREATEDATUTC = args.CreateDate,
                ISCODEJOINENABLED = true 
            };

            var matchPlayer = new MATCH_PLAYER
            {
                MATCH = match,
                USERID = args.UserProfileId,
                SLOTNUMBER = HOST_SLOT_NUMBER,
                ISHOST = true,
                ISREADY = true,
                JOINEDATUTC = args.CreateDate
            };

            _dataContext.MATCH.Add(match);
            _dataContext.MATCH_PLAYER.Add(matchPlayer);

            _dataContext.SaveChanges();

            return MapToDto(match);
        }

        public StartMatchResult StartMatch(long matchId)
        {
            if (matchId <= 0)
            {
                return StartMatchResult.MatchNotFound;
            }

            MATCH match = _dataContext.MATCH.SingleOrDefault(m => m.MATCHID == matchId);
            if (match == null)
            {
                return StartMatchResult.MatchNotFound;
            }

            if (!IsLobbyMatch(match))
            {
                return StartMatchResult.MatchNotInLobby;
            }

            var activePlayers = GetActivePlayersForMatch(_dataContext, matchId).ToList();

            if (activePlayers.Count < MIN_PLAYERS_TO_START)
            {
                return StartMatchResult.NotEnoughPlayers;
            }

            if (activePlayers.Count > MAX_MATCH_PLAYERS_BY_SCHEMA || HasDuplicateActiveSlot(activePlayers))
            {
                return StartMatchResult.NotEnoughPlayers;
            }

            if (activePlayers.Any(p => p == null || !p.ISREADY))
            {
                return StartMatchResult.PlayersNotReady;
            }

            DateTime utcNow = DateTime.UtcNow;

            match.STATUSID = MatchStatusIds.ACTIVE;
            match.STARTTIME = utcNow;

            _dataContext.SaveChanges();

            return StartMatchResult.Success;
        }

        public EndMatchResult EndMatch(EndMatchArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            MATCH match = _dataContext.MATCH.SingleOrDefault(m => m.MATCHID == args.MatchId);
            if (match == null)
            {
                return EndMatchResult.MatchNotFound;
            }

            if (!IsActiveMatch(match))
            {
                return EndMatchResult.MatchNotInProgress;
            }

            var players = GetPlayersForMatch(_dataContext, args.MatchId).ToList();
            MATCH_PLAYER winner = players.SingleOrDefault(p => p != null && p.USERID == args.WinnerUserId);

            if (winner == null)
            {
                return EndMatchResult.WinnerNotInMatch;
            }

            DateTime utcNow = DateTime.UtcNow;

            FinalizeMatchWithWinner(match, players, winner, utcNow);

            _dataContext.SaveChanges();

            return EndMatchResult.Success;
        }

        public MatchDto GetOpenMatchByCode(string matchCode)
        {
            string safeCode = matchCode ?? string.Empty;
            if (string.IsNullOrWhiteSpace(safeCode))
            {
                return MatchDto.CreateInvalid();
            }

            MATCH match = _dataContext.MATCH.AsNoTracking().SingleOrDefault(m =>
                m.MATCHCODE == safeCode &&
                m.STATUSID == MatchStatusIds.LOBBY &&
                m.STARTTIME == null &&
                m.ENDTIME == null,
                m.ISCODEJOINENABLED == true 
            );

            return match == null ? MatchDto.CreateInvalid() : MapToDto(match);
        }

        public long CreateMatchForTournamentQuick(long player1UserId, long player2UserId, DateTime utcNow)
        {
            if (player1UserId <= 0 || player2UserId <= 0 || player1UserId == player2UserId)
            {
                return MatchDto.INVALID_MATCH_ID;
            }

            string code = GenerateMatchCode();

            var match = new MATCH
            {
                VISIBILITYID = MatchVisibilityIds.PRIVATE,
                STATUSID = MatchStatusIds.ACTIVE,
                MODEID = MatchModeIds.QUICK,
                MATCHCODE = code,
                CREATEDATUTC = utcNow,
                STARTTIME = utcNow,
                ISCODEJOINENABLED = false 
            };

            _dataContext.MATCH.Add(match);

            _dataContext.MATCH_PLAYER.Add(new MATCH_PLAYER
            {
                MATCH = match,
                USERID = player1UserId,
                SLOTNUMBER = HOST_SLOT_NUMBER,
                ISHOST = true,
                ISREADY = true,
                JOINEDATUTC = utcNow
            });

            _dataContext.MATCH_PLAYER.Add(new MATCH_PLAYER
            {
                MATCH = match,
                USERID = player2UserId,
                SLOTNUMBER = GUEST_SLOT_NUMBER,
                ISHOST = false,
                ISREADY = true,
                JOINEDATUTC = utcNow
            });

            _dataContext.SaveChanges();

            return match.MATCHID;
        }

        private string GenerateMatchCode()
        {
            const int bufferLength = MATCH_CODE_LENGTH;
            byte[] buffer = new byte[bufferLength];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            char[] chars = new char[MATCH_CODE_LENGTH];

            for (int i = 0; i < MATCH_CODE_LENGTH; i++)
            {
                int index = buffer[i] % MATCH_CODE_ALPHABET.Length;
                chars[i] = MATCH_CODE_ALPHABET[index];
            }

            return new string(chars);
        }

        private MatchDto MapToDto(MATCH match)
        {
            if (match == null)
            {
                return MatchDto.CreateInvalid();
            }

            return new MatchDto
            {
                MatchId = match.MATCHID,
                Code = match.MATCHCODE ?? string.Empty,
                StatusId = (MatchStatus)match.STATUSID,
                VisibilityId = (MatchVisibility)match.VISIBILITYID,
                CreateAtUtc = match.CREATEDATUTC,
                Mode = match.MODEID
            };
        }
    }
}
