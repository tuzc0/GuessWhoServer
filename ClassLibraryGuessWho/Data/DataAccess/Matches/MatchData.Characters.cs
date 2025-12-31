using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
using System.Data.Entity; 
using System.Data.SqlClient;
using System.Linq;

namespace ClassLibraryGuessWho.Data.DataAccess.Matches
{
    public sealed partial class MatchData
    {
        private const string SQL_CHOOSE_SECRET_CHARACTER_ATOMIC = @"UPDATE MP SET MP.SECRETCHARACTERID = @CharacterId 
                                                                  FROM MATCH_PLAYER MP
                                                                  INNER JOIN MATCH M ON M.MATCHID = MP.MATCHID
                                                                  WHERE MP.MATCHID = @MatchId
                                                                  AND MP.USERID = @UserId
                                                                  AND MP.LEFTATUTC IS NULL
                                                                  AND M.STATUSID = @MatchStatusActive
                                                                  AND (MP.SECRETCHARACTERID IS NULL OR LTRIM(RTRIM(MP.SECRETCHARACTERID)) = '')
                                                                  AND EXISTS (
                                                                      SELECT 1
                                                                      FROM CHARACTER C
                                                                      WHERE C.CHARACTERID = @CharacterId
                                                                      AND C.ISACTIVE = 1);";

        private const string SQL_CHANGE_SECRET_CHARACTER_ATOMIC = @"UPDATE MP SET MP.SECRETCHARACTERID = @CharacterId 
                                                                  FROM MATCH_PLAYER MP
                                                                  INNER JOIN MATCH M ON M.MATCHID = MP.MATCHID
                                                                  WHERE MP.MATCHID = @MatchId
                                                                  AND MP.USERID = @UserId
                                                                  AND MP.LEFTATUTC IS NULL
                                                                  AND M.STATUSID = @MatchStatusLobby
                                                                  AND EXISTS (
                                                                      SELECT 1
                                                                      FROM CHARACTER C
                                                                      WHERE C.CHARACTERID = @CharacterId
                                                                      AND C.ISACTIVE = 1);";

        public ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.MatchId <= 0)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotFound);
            }

            if (args.UserProfileId <= 0)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerNotInMatch);
            }

            string characterId = (args.SecretCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(characterId))
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.InvalidCharacter);
            }

            int affectedRows = dataContext.Database.ExecuteSqlCommand( 
                SQL_CHOOSE_SECRET_CHARACTER_ATOMIC,
                new SqlParameter("@CharacterId", characterId), 
                new SqlParameter("@MatchId", args.MatchId),
                new SqlParameter("@UserId", args.UserProfileId),
                new SqlParameter("@MatchStatusActive", MatchStatusIds.ACTIVE));

            if (affectedRows > 0)
            {
                return ChooseSecretCharacterResult.Success();
            }

            ChooseSecretDiagnostic diagnostic = LoadChooseSecretDiagnostic(
                args.MatchId,
                args.UserProfileId,
                characterId); 

            if (diagnostic == null)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotFound);
            }

            if (diagnostic.MatchStatusId != MatchStatusIds.ACTIVE)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotInProgress);
            }

            if (!diagnostic.PlayerExists)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerNotInMatch);
            }

            if (diagnostic.PlayerLeftAtUtc != null)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerAlreadyLeft);
            }

            if (diagnostic.HasSecretCharacterChosen)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.SecretAlreadyChosen);
            }

            if (!diagnostic.CharacterExists)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.InvalidCharacter);
            }

            return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.OperationConflict);
        }

        public ChangeSecretCharacterResult ChangeSecretCharacter(ChangeSecretCharacterArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.MatchId <= 0)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotFound);
            }

            if (args.UserId <= 0)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerNotInMatch);
            }

            string characterId = (args.SecretCharacterId ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(characterId))
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.InvalidCharacter);
            }

            int affectedRows = dataContext.Database.ExecuteSqlCommand(
                SQL_CHANGE_SECRET_CHARACTER_ATOMIC,
                new SqlParameter("@CharacterId", characterId),
                new SqlParameter("@MatchId", args.MatchId),
                new SqlParameter("@UserId", args.UserId),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY));

            if (affectedRows > 0)
            {
                return ChangeSecretCharacterResult.Success();
            }

            ChangeSecretDiagnostic diagnostic = LoadChangeSecretDiagnostic(
                args.MatchId,
                args.UserId,
                characterId);

            if (diagnostic == null)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotFound);
            }

            if (diagnostic.MatchStatusId != MatchStatusIds.LOBBY)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotInLobby);
            }

            if (!diagnostic.PlayerExists)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerNotInMatch);
            }

            if (diagnostic.PlayerLeftAtUtc != null)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerAlreadyLeft);
            }

            if (!diagnostic.CharacterExists)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.InvalidCharacter);
            }

            return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.OperationConflict);
        }

        private sealed class ChooseSecretDiagnostic
        {
            public byte MatchStatusId { get; set; }
            public bool PlayerExists { get; set; }
            public DateTime? PlayerLeftAtUtc { get; set; }
            public bool HasSecretCharacterChosen { get; set; }
            public bool CharacterExists { get; set; }
        }

        private ChooseSecretDiagnostic LoadChooseSecretDiagnostic(long matchId, long userId, string characterId) 
        {
            byte? matchStatusId = dataContext.MATCH.AsNoTracking()
                .Where(m => m.MATCHID == matchId)
                .Select(m => (byte?)m.STATUSID)
                .SingleOrDefault();

            if (!matchStatusId.HasValue)
            {
                return null;
            }

            var player = dataContext.MATCH_PLAYER.AsNoTracking()
                .Where(p => p.MATCHID == matchId && p.USERID == userId)
                .Select(p => new
                {
                    p.LEFTATUTC,
                    p.SECRETCHARACTERID
                })
                .SingleOrDefault();

            bool characterExists = dataContext.CHARACTER.AsNoTracking().Any(c =>
                c.CHARACTERID == characterId && c.ISACTIVE);

            return new ChooseSecretDiagnostic
            {
                MatchStatusId = matchStatusId.Value,
                PlayerExists = player != null,
                PlayerLeftAtUtc = player != null ? player.LEFTATUTC : (DateTime?)null,
                HasSecretCharacterChosen = player != null && !string.IsNullOrWhiteSpace((player.SECRETCHARACTERID ?? string.Empty).Trim()),
                CharacterExists = characterExists
            };
        }

        private sealed class ChangeSecretDiagnostic
        {
            public byte MatchStatusId { get; set; }
            public bool PlayerExists { get; set; }
            public DateTime? PlayerLeftAtUtc { get; set; }
            public bool CharacterExists { get; set; }
        }

        private ChangeSecretDiagnostic LoadChangeSecretDiagnostic(long matchId, long userId, string characterId) 
        {
            byte? matchStatusId = dataContext.MATCH.AsNoTracking()
                .Where(m => m.MATCHID == matchId)
                .Select(m => (byte?)m.STATUSID)
                .SingleOrDefault();

            if (!matchStatusId.HasValue)
            {
                return null;
            }

            var player = dataContext.MATCH_PLAYER.AsNoTracking()
                .Where(p => p.MATCHID == matchId && p.USERID == userId)
                .Select(p => new
                {
                    p.LEFTATUTC
                })
                .SingleOrDefault();

            bool characterExists = dataContext.CHARACTER.AsNoTracking().Any(c =>
                c.CHARACTERID == characterId && c.ISACTIVE);

            return new ChangeSecretDiagnostic
            {
                MatchStatusId = matchStatusId.Value,
                PlayerExists = player != null,
                PlayerLeftAtUtc = player != null ? player.LEFTATUTC : (DateTime?)null,
                CharacterExists = characterExists
            };
        }
    }
}
