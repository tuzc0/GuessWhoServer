using GuessWhoServerDomain.Domain.Enums.Match;
using GuessWhoServerDomain.Domain.Parameters.Matches;
using GuessWhoServerDomain.Domain.Results.Match;
using System;
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

        public ChooseSecretCharacterResult ChooseSecretCharacter(ChooseSecretCharacterArgs chooseSecretCharacterArgs)
        {
            if (chooseSecretCharacterArgs == null)
            {
                throw new ArgumentNullException(nameof(chooseSecretCharacterArgs));
            }

            if (chooseSecretCharacterArgs.MatchId <= 0)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotFound);
            }

            if (chooseSecretCharacterArgs.UserProfileId <= 0)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerNotInMatch);
            }

            if (string.IsNullOrWhiteSpace(chooseSecretCharacterArgs.SecretCharacterId))
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.InvalidCharacter);
            }

            int affectdRows = dataContext.Database.ExecuteSqlCommand(
                SQL_CHOOSE_SECRET_CHARACTER_ATOMIC,
                new SqlParameter("@CharacterId", chooseSecretCharacterArgs.SecretCharacterId),
                new SqlParameter("@MatchId", chooseSecretCharacterArgs.MatchId),
                new SqlParameter("@UserId", chooseSecretCharacterArgs.UserProfileId),
                new SqlParameter("@MatchStatusActive", MatchStatusIds.ACTIVE));

            if (affectdRows > 0)
            {
                return ChooseSecretCharacterResult.Success();
            }

            var diagnosticData = (from m in dataContext.MATCH.AsNoTracking()
                        where m.MATCHID == chooseSecretCharacterArgs.MatchId
                        from mp in dataContext.MATCH_PLAYER.AsNoTracking()
                        .Where(p => p.MATCHID == m.MATCHID && p.USERID == chooseSecretCharacterArgs.UserProfileId)
                        .DefaultIfEmpty()
                        select new
                        {
                            MatchStatusId = m.STATUSID,
                            PlayerExists = mp != null,
                            PlayerLeftAtUtc = mp.LEFTATUTC,
                            CurrentSecretCharacterId = mp.SECRETCHARACTERID,
                            CharacterExists = dataContext.CHARACTER.AsNoTracking().Any(c =>
                            c.CHARACTERID == chooseSecretCharacterArgs.SecretCharacterId &&
                            c.ISACTIVE)
                        }).SingleOrDefault();

            if (diagnosticData == null)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotFound);
            }

            if (diagnosticData.MatchStatusId != MatchStatusIds.ACTIVE)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.MatchNotInProgress);
            }

            if (!diagnosticData.PlayerExists)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerNotInMatch);
            }

            if (diagnosticData.PlayerLeftAtUtc != null)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.PlayerAlreadyLeft);
            }

            if (!string.IsNullOrWhiteSpace(diagnosticData.CurrentSecretCharacterId))
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.SecretAlreadyChosen);
            }

            if (!diagnosticData.CharacterExists)
            {
                return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.InvalidCharacter);
            }

            return ChooseSecretCharacterResult.Fail(ChooseSecretCharacterResultCode.TechnicalError);
        }

        public ChangeSecretCharacterResult ChangeSecretCharacter(ChangeSecretCharacterArgs changeSecretCharacterArgs)
        {
            if (changeSecretCharacterArgs == null)
            {
                throw new ArgumentNullException(nameof(changeSecretCharacterArgs));
            }

            if (changeSecretCharacterArgs.MatchId <= 0)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotFound);
            }

            if (changeSecretCharacterArgs.UserId <= 0)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerNotInMatch);
            }

            if (string.IsNullOrWhiteSpace(changeSecretCharacterArgs.SecretCharacterId))
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.InvalidCharacter);
            }

            int affectedRows = dataContext.Database.ExecuteSqlCommand(SQL_CHANGE_SECRET_CHARACTER_ATOMIC,
                new SqlParameter("@CharacterId", changeSecretCharacterArgs.SecretCharacterId),
                new SqlParameter("@MatchId", changeSecretCharacterArgs.MatchId),
                new SqlParameter("@UserId", changeSecretCharacterArgs.UserId),
                new SqlParameter("@MatchStatusLobby", MatchStatusIds.LOBBY));

            if (affectedRows > 0)
            {
                return ChangeSecretCharacterResult.Success();
            }

            var diagnosticData = (from m in dataContext.MATCH.AsNoTracking() 
                        where m.MATCHID == changeSecretCharacterArgs.MatchId
                        from mp in dataContext.MATCH_PLAYER.AsNoTracking()
                        .Where(p => p.MATCHID == m.MATCHID && p.USERID == changeSecretCharacterArgs.UserId)
                        .DefaultIfEmpty()
                        select new
                        {
                            MatchStatusId = m.STATUSID,
                            PlayerExists = mp != null,
                            PlayerLeftAtUtc = mp.LEFTATUTC,
                            CharacterExists = dataContext.CHARACTER.AsNoTracking().Any(c =>
                            c.CHARACTERID == changeSecretCharacterArgs.SecretCharacterId &&
                            c.ISACTIVE)
                        }).SingleOrDefault();

            if (diagnosticData == null)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotFound);
            }

            if (diagnosticData.MatchStatusId != MatchStatusIds.LOBBY)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.MatchNotInLobby);
            }

            if (!diagnosticData.PlayerExists)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerNotInMatch);
            }

            if (diagnosticData.PlayerLeftAtUtc != null)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.PlayerAlreadyLeft);
            }

            if (!diagnosticData.CharacterExists)
            {
                return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.InvalidCharacter);
            }

            return ChangeSecretCharacterResult.Fail(ChangeSecretCharacterResultCode.TechnicalError);
        }
    }
}
