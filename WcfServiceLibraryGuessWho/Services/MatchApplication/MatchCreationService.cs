using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWho.Services.Security;
using GuessWhoContracts.Dtos.Dto;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Data.Entity.Infrastructure;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public class MatchCreationService : IMatchCreationService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchCreationService));

        private readonly MatchData matchData;

        private const string ERROR_INVALID_REQUEST = "InvalidRequest";
        private const string ERROR_FOREIGN_KEY = "ForeignKey";
        private const string ERROR_DATABASE_TIMEOUT = "DatabaseTimeout";
        private const string ERROR_DATABASE_CONNECTION = "DatabaseConnection";
        private const string ERROR_DATABASE_UPDATE = "DatabaseUpdate";
        private const string ERROR_DATABASE_SQL = "DatabaseSql";

        private const string MESSAGE_REQUEST_NULL = "Request cannot be null.";
        private const string MESSAGE_FOREIGN_KEY = "Operation violates an existing relation.";
        private const string MESSAGE_DATABASE_TIMEOUT = "The database did not respond in time.";
        private const string MESSAGE_DATABASE_CONNECTION = "Unable to connect to the database.";
        private const string MESSAGE_DATABASE_UPDATE = "A database update error occurred.";
        private const string MESSAGE_DATABASE_SQL = "A database error occurred.";

        public MatchCreationService(MatchData matchData)
        {
            this.matchData = matchData ?? throw new ArgumentNullException(nameof(matchData));

        }

        public CreateMatchResponse CreateMatch(CreateMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(ERROR_INVALID_REQUEST, MESSAGE_REQUEST_NULL);
            }

            var dateNowUtc = DateTime.UtcNow;
            var visibilityDefault = MatchVisibility.Public;
            var statusDefault = MatchStatus.Lobby;
            var modeDefault = MatchMode.Classic;
            long hostUserId = request.ProfileId;

            try
            {
                string matchCode = CodeGenerator.GenerateNumericCode();

                var createMatchArgs = new CreateMatchArgs
                {
                    UserProfileId = hostUserId,
                    Visibility = (byte)visibilityDefault,
                    MatchStatus = (byte)statusDefault,
                    Mode = (byte) modeDefault,
                    CreateDate = dateNowUtc,
                    MatchCode = matchCode
                };

                MatchDto match = matchData.CreateMatchClassic(createMatchArgs);
                var players = matchData.GetMatchPlayers(match.MatchId);

                return new CreateMatchResponse
                {
                    MatchId = match.MatchId,
                    Code = match.Code,
                    StatusId = match.StatusId,
                    Mode = match.Mode,
                    Visibility = match.VisibilityId,
                    CreateAtUtc = match.CreateAtUtc,
                    Players = players
                };
            }
            catch (DbUpdateException ex)
            {
                return HandleDatabaseExceptionForCreateMatch(ex);
            }
            catch (Exception ex)
            {
                return HandleInfrastructureExceptionForCreateMatch(ex);
            }
        }

        private const string LOG_DB_TIMEOUT_CREATE_MATCH = "Database command timeout in MatchCreationService.CreateMatch.";
        private const string LOG_DB_CONNECTION_CREATE_MATCH = "Database connection failure in MatchCreationService.CreateMatch.";
        private const string LOG_DB_UPDATE_CREATE_MATCH = "Database update error in MatchCreationService.CreateMatch.";
        private const string LOG_DB_UNEXPECTED_CREATE_MATCH = "Unexpected DB error in MatchCreationService.CreateMatch.";
        private const string LOG_UNEXPECTED_CREATE_MATCH = "Unexpected error in MatchCreationService.CreateMatch.";

        private CreateMatchResponse HandleDatabaseExceptionForCreateMatch(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_CREATE_MATCH, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_CREATE_MATCH, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);

                    case SqlErrorKind.ForeignKeyViolation:
                        Logger.Error("Foreign key violation in MatchCreationService.CreateMatch.", ex);
                        throw Faults.Create(ERROR_FOREIGN_KEY, MESSAGE_FOREIGN_KEY);
                }
            }

            Logger.Fatal(LOG_DB_UPDATE_CREATE_MATCH, ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private CreateMatchResponse HandleInfrastructureExceptionForCreateMatch(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_CREATE_MATCH, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_CREATE_MATCH, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_CREATE_MATCH, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }
    }
}
