using ClassLibraryGuessWho.Data.DataAccess.Match;
using ClassLibraryGuessWho.Data.DataAccess.Match.Parameters;
using ClassLibraryGuessWho.Data.Helpers;
using GuessWhoContracts.Dtos.RequestAndResponse;
using GuessWhoContracts.Enums;
using GuessWhoContracts.Faults;
using log4net;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.ServiceModel;
using WcfServiceLibraryGuessWho.Services.MatchApplication;

namespace GuessWho.Services.WCF.Services.MatchApplication
{
    public sealed class MatchLifecycleService : IMatchLifecycleService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchLifecycleService));

        private const string ERROR_INVALID_REQUEST = "InvalidRequest";
        private const string ERROR_DATABASE_TIMEOUT = "DatabaseTimeout";
        private const string ERROR_DATABASE_CONNECTION = "DatabaseConnection";
        private const string ERROR_DATABASE_UPDATE = "DatabaseUpdate";
        private const string ERROR_DATABASE_SQL = "DatabaseSql";
        private const string ERROR_NOT_ENOUGH_PLAYERS = "NotEnoughPlayers";
        private const string ERROR_PLAYERS_NOT_READY = "PlayersNotReady";
        private const string ERROR_MATCH_NOT_FOUND = "MatchNotFound";
        private const string ERROR_UNKNOWN = "UnknownError";
        private const string ERROR_MATCH_NOT_IN_LOBBY = "MatchNotInLobby";
        private const string ERROR_MATCH_NOT_IN_PROGRESS = "MatchNotInProgress";
        private const string ERROR_WINNER_NOT_IN_MATCH = "WinnerNotInMatch";

        private const string ERROR_PLAYER_NOT_IN_MATCH = "PlayerNotInMatch";
        private const string ERROR_PLAYER_ALREADY_LEFT = "PlayerAlreadyLeft";
        private const string ERROR_INVALID_CHARACTER = "InvalidCharacter";
        private const string ERROR_SECRET_ALREADY_CHOSEN = "SecretAlreadyChosen";

        private const string MESSAGE_REQUEST_NULL = "Request cannot be null.";
        private const string MESSAGE_DATABASE_TIMEOUT = "The database did not respond in time.";
        private const string MESSAGE_DATABASE_CONNECTION = "Unable to connect to the database.";
        private const string MESSAGE_DATABASE_UPDATE = "A database update error occurred.";
        private const string MESSAGE_DATABASE_SQL = "A database error occurred.";

        private const string MESSAGE_NOT_ENOUGH_PLAYERS = "There are not enough players to start the match.";
        private const string MESSAGE_PLAYERS_NOT_READY = "All players must be ready to start the match.";
        private const string MESSAGE_UNKNOWN_START_ERROR = "The match could not be started due to an unknown error.";
        private const string MESSAGE_MATCH_NOT_FOUND = "The match does not exist.";
        private const string MESSAGE_MATCH_NOT_IN_LOBBY = "The match is no longer in the lobby or has already started.";
        private const string MESSAGE_MATCH_NOT_IN_PROGRESS = "The match is not in progress.";
        private const string MESSAGE_WINNER_NOT_IN_MATCH = "The specified winner does not belong to this match.";
        private const string MESSAGE_UNKNOWN_END_ERROR = "The match could not be finished due to an unknown error.";

        private const string MESSAGE_CHOOSE_SECRET_REQUEST_NULL = "ChooseSecretCharacter request cannot be null.";
        private const string MESSAGE_PLAYER_NOT_IN_MATCH = "User does not belong to this match.";
        private const string MESSAGE_PLAYER_ALREADY_LEFT = "User had already left the match.";
        private const string MESSAGE_INVALID_CHARACTER = "Selected character is not valid.";
        private const string MESSAGE_SECRET_ALREADY_CHOSEN = "Secret character was already chosen.";
        private const string MESSAGE_UNKNOWN_CHOOSE_SECRET_ERROR = "Secret character could not be selected due to an unknown error.";

        private readonly MatchData matchData;
        private readonly IMatchDeckProvider matchDeckProvider;
        private readonly ILobbyNotifier lobbyNotifier;

        public MatchLifecycleService(
            MatchData matchData, 
            ILobbyNotifier lobbyNotifier, 
            IMatchDeckProvider matchDeckProvider)
        {
            this.matchData = matchData ?? 
                throw new ArgumentNullException(nameof(matchData));
            this.lobbyNotifier = lobbyNotifier ?? 
                throw new ArgumentNullException(nameof(lobbyNotifier));
            this.matchDeckProvider = matchDeckProvider ?? 
                throw new ArgumentNullException(nameof(matchDeckProvider));
        }

        public BasicResponse StartMatch(StartMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_REQUEST_NULL);
            }

            try
            {
                StartMatchResult result = matchData.StartMatch(request.MatchId);

                switch (result)
                {
                    case StartMatchResult.Success:

                        lobbyNotifier.NotifyGameStarted(request.MatchId);

                        return new BasicResponse
                        {
                            Success = true
                        };

                    case StartMatchResult.MatchNotFound:

                        Logger.WarnFormat("StartMatch: match not found. MatchId={0}",
                            request.MatchId);
                        
                        throw Faults.Create(
                            ERROR_MATCH_NOT_FOUND,
                            MESSAGE_MATCH_NOT_FOUND);

                    case StartMatchResult.MatchNotInLobby:

                        Logger.WarnFormat("StartMatch: match is not in lobby state. MatchId={0}",
                            request.MatchId);
                        
                        throw Faults.Create(
                            ERROR_MATCH_NOT_IN_LOBBY,
                            MESSAGE_MATCH_NOT_IN_LOBBY);

                    case StartMatchResult.NotEnoughPlayers:

                        Logger.InfoFormat("StartMatch: not enough players to start. MatchId={0}",
                            request.MatchId);
                        
                        throw Faults.Create(
                            ERROR_NOT_ENOUGH_PLAYERS,
                            MESSAGE_NOT_ENOUGH_PLAYERS);

                    case StartMatchResult.PlayersNotReady:

                        Logger.InfoFormat("StartMatch: some players are not ready. MatchId={0}",
                            request.MatchId);
                        
                        throw Faults.Create(
                            ERROR_PLAYERS_NOT_READY,
                            MESSAGE_PLAYERS_NOT_READY);

                    default:

                        Logger.ErrorFormat("StartMatch: unknown result. MatchId={0}, Result={1}",
                            request.MatchId, result);
                        throw Faults.Create(
                            ERROR_UNKNOWN,
                            MESSAGE_UNKNOWN_START_ERROR);
                }
            }
            catch (FaultException<ServiceFault>) 
            { 
                throw; 
            }
            catch (DbUpdateException ex) 
            {
                return HandleDatabaseExceptionForStartMatch(ex); 
            }
            catch (Exception ex) 
            { 
                return HandleInfrastructureExceptionForStartMatch(ex);
            }
        }

        public BasicResponse EndMatch(EndMatchRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_REQUEST_NULL);
            }

            var args = new EndMatchArgs
            {
                MatchId = request.MatchId,
                WinnerUserId = request.WinnerUserId
            };

            try
            {
                EndMatchResult result = matchData.EndMatch(args);

                switch (result)
                {
                    case EndMatchResult.Success:

                        lobbyNotifier.NotifyGameEnded(request.MatchId, request.WinnerUserId);

                        return new BasicResponse
                        {
                            Success = true
                        };

                    case EndMatchResult.MatchNotFound:

                        Logger.WarnFormat("EndMatch: match not found. MatchId={0}",
                            request.MatchId);

                        throw Faults.Create(
                            ERROR_MATCH_NOT_FOUND,
                            MESSAGE_MATCH_NOT_FOUND);

                    case EndMatchResult.MatchNotInProgress:

                        Logger.WarnFormat("EndMatch: match is not in progress. MatchId={0}",
                            request.MatchId);

                        throw Faults.Create(
                            ERROR_MATCH_NOT_IN_PROGRESS,
                            MESSAGE_MATCH_NOT_IN_PROGRESS);

                    case EndMatchResult.WinnerNotInMatch:

                        Logger.WarnFormat("EndMatch: winner does not belong to match. " +
                            "MatchId={0}, WinnerUserId={1}",
                            request.MatchId, request.WinnerUserId);

                        throw Faults.Create(
                            ERROR_WINNER_NOT_IN_MATCH,
                            MESSAGE_WINNER_NOT_IN_MATCH);

                    default:

                        Logger.ErrorFormat("EndMatch: unknown result. MatchId={0}, Result={1}",
                            request.MatchId, result);

                        throw Faults.Create(
                            ERROR_UNKNOWN,
                            MESSAGE_UNKNOWN_END_ERROR);
                }
            }
            catch (FaultException<ServiceFault>) 
            { 
                throw; 
            }
            catch (DbUpdateException ex) 
            { 
                return HandleDatabaseExceptionForEndMatch(ex); 
            }
            catch (Exception ex) 
            { 
                return HandleInfrastructureExceptionForEndMatch(ex); 
            }
        }

        public BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_CHOOSE_SECRET_REQUEST_NULL);
            }

            var args = new ChooseSecretCharacterArgs
            {
                MatchId = request.MatchId,
                UserProfileId = request.UserId,
                SecretCharacterId = request.CharacterId
            };

            try
            {
                ChooseSecretCharacterResult result = matchData.ChooseSecretCharacter(args);

                switch (result)
                {
                    case ChooseSecretCharacterResult.Success:

                        lobbyNotifier.NotifySecretCharacterChosen(request.MatchId, request.UserId);

                        bool allSecretsChosen = matchData.AreAllSecretCharactersChosen(request.MatchId);

                        if (allSecretsChosen)
                        {
                            lobbyNotifier.NotifyAllSecretCharactersChosen(request.MatchId);
                        }

                        return new BasicResponse
                        {
                            Success = true
                        };

                    case ChooseSecretCharacterResult.MatchNotFound:

                        Logger.WarnFormat("ChooseSecretCharacter: match not found. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                       
                        throw Faults.Create(
                            ERROR_MATCH_NOT_FOUND,
                            MESSAGE_MATCH_NOT_FOUND);

                    case ChooseSecretCharacterResult.MatchNotInProgress:
                        
                        Logger.WarnFormat("ChooseSecretCharacter: match is not in progress. MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        
                        throw Faults.Create(
                            ERROR_MATCH_NOT_IN_PROGRESS,
                            MESSAGE_MATCH_NOT_IN_PROGRESS);

                    case ChooseSecretCharacterResult.PlayerNotInMatch:

                        Logger.WarnFormat("ChooseSecretCharacter: user is not in match. " +
                            "MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        
                        throw Faults.Create(
                            ERROR_PLAYER_NOT_IN_MATCH,
                            MESSAGE_PLAYER_NOT_IN_MATCH);

                    case ChooseSecretCharacterResult.PlayerAlreadyLeft:

                        Logger.InfoFormat("ChooseSecretCharacter: user had already left match. " +
                            "MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        
                        throw Faults.Create(
                            ERROR_PLAYER_ALREADY_LEFT,
                            MESSAGE_PLAYER_ALREADY_LEFT);

                    case ChooseSecretCharacterResult.InvalidCharacter:
                        
                        Logger.WarnFormat("ChooseSecretCharacter: invalid character. " +
                            "MatchId={0}, UserId={1}, CharacterId={2}",
                            request.MatchId, request.UserId, request.CharacterId);
                        
                        throw Faults.Create(
                            ERROR_INVALID_CHARACTER,
                            MESSAGE_INVALID_CHARACTER);

                    case ChooseSecretCharacterResult.SecretAlreadyChosen:

                        Logger.InfoFormat("ChooseSecretCharacter: user had already chosen a secret character. " +
                            "MatchId={0}, UserId={1}",
                            request.MatchId, request.UserId);
                        
                        throw Faults.Create(
                            ERROR_SECRET_ALREADY_CHOSEN,
                            MESSAGE_SECRET_ALREADY_CHOSEN);

                    default:

                        Logger.ErrorFormat("ChooseSecretCharacter: unknown result. " +
                            "MatchId={0}, UserId={1}, CharacterId={2}, Result={3}",
                            request.MatchId, request.UserId, request.CharacterId,
                            result);
                        
                        throw Faults.Create(
                            ERROR_UNKNOWN,
                            MESSAGE_UNKNOWN_CHOOSE_SECRET_ERROR);
                }
            }
            catch (FaultException<ServiceFault>) 
            { 
                throw; 
            }
            catch (DbUpdateException ex) 
            { 
                return HandleDatabaseExceptionForChooseSecret(ex); 
            }
            catch (Exception ex) 
            { 
                return HandleInfrastructureExceptionForChooseSecret(ex); 
            }
        }

        public MatchDeckResponse GetMatchDeck(GetMatchDeckRequest request)
        {
            if (request == null)
            {
                throw Faults.Create(
                    ERROR_INVALID_REQUEST,
                    MESSAGE_REQUEST_NULL);
            }

            try
            {
                string[] characterIds = matchDeckProvider.GetMatchDeck(
                    request.MatchId, request.NumberOfCardsInDeck);

                return new MatchDeckResponse
                {
                    CharacterIds = characterIds
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Fatal(string.Format("GetMatchDeck: unexpected error. MatchId={0}",
                        request.MatchId), ex);

                throw Faults.Create(
                    ERROR_UNKNOWN,
                    "The match deck could not be obtained due to an unexpected error.");
            }
        }
    
        private const string LOG_DB_TIMEOUT_START = "Database command timeout in MatchLifecycleService.StartMatch.";
        private const string LOG_DB_CONNECTION_START = "Database connection failure in MatchLifecycleService.StartMatch.";
        private const string LOG_DB_UNEXPECTED_START = "Unexpected DB error in MatchLifecycleService.StartMatch.";

        private const string LOG_DB_TIMEOUT_END = "Database command timeout in MatchLifecycleService.EndMatch.";
        private const string LOG_DB_CONNECTION_END = "Database connection failure in MatchLifecycleService.EndMatch.";
        private const string LOG_DB_UNEXPECTED_END = "Unexpected DB error in MatchLifecycleService.EndMatch.";

        private const string LOG_DB_TIMEOUT_CHOOSE = "Database command timeout in MatchLifecycleService.ChooseSecretCharacter.";
        private const string LOG_DB_CONNECTION_CHOOSE = "Database connection failure in MatchLifecycleService.ChooseSecretCharacter.";
        private const string LOG_DB_UNEXPECTED_CHOOSE = "Unexpected DB error in MatchLifecycleService.ChooseSecretCharacter.";

        private BasicResponse HandleDatabaseExceptionForStartMatch(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_START, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_START, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal("Database update error in MatchLifecycleService.StartMatch.", ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private BasicResponse HandleInfrastructureExceptionForStartMatch(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_START, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_START, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_START, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }

        private BasicResponse HandleDatabaseExceptionForEndMatch(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_END, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_END, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal("Database update error in MatchLifecycleService.EndMatch.", ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private BasicResponse HandleInfrastructureExceptionForEndMatch(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_END, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_END, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_END, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }

        private BasicResponse HandleDatabaseExceptionForChooseSecret(DbUpdateException ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_CHOOSE, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_CHOOSE, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal("Database update error in MatchLifecycleService.ChooseSecretCharacter.", ex);
            throw Faults.Create(ERROR_DATABASE_UPDATE, MESSAGE_DATABASE_UPDATE);
        }

        private BasicResponse HandleInfrastructureExceptionForChooseSecret(Exception ex)
        {
            var errorInfo = SqlExceptionInspector.Inspect(ex);

            if (errorInfo.HasSqlException)
            {
                switch (errorInfo.Kind)
                {
                    case SqlErrorKind.Timeout:
                        Logger.Fatal(LOG_DB_TIMEOUT_CHOOSE, ex);
                        throw Faults.Create(ERROR_DATABASE_TIMEOUT, MESSAGE_DATABASE_TIMEOUT);

                    case SqlErrorKind.ConnectionFailure:
                    case SqlErrorKind.DatabaseNotFound:
                        Logger.Fatal(LOG_DB_CONNECTION_CHOOSE, ex);
                        throw Faults.Create(ERROR_DATABASE_CONNECTION, MESSAGE_DATABASE_CONNECTION);
                }
            }

            Logger.Fatal(LOG_DB_UNEXPECTED_CHOOSE, ex);
            throw Faults.Create(ERROR_DATABASE_SQL, MESSAGE_DATABASE_SQL);
        }
    }
}
