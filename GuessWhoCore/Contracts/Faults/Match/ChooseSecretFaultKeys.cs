namespace GuessWhoCore.Contracts.Faults.Match
{
    public static class ChooseSecretFaultKeys
    {
        public const string CODE_INVALID_REQUEST = "MATCH_INVALID_REQUEST";
        public const string MSG_INVALID_REQUEST = "Match.InvalidRequest";
        public const string FALLBACK_INVALID_REQUEST = "Invalid request.";

        public const string CODE_INVALID_MATCH_ID = "MATCH_INVALID_MATCH_ID";
        public const string MSG_INVALID_MATCH_ID = "Match.InvalidMatchId";
        public const string FALLBACK_INVALID_MATCH_ID = "Invalid match id.";

        public const string CODE_INVALID_USER_ID = "MATCH_INVALID_USER_ID";
        public const string MSG_INVALID_USER_ID = "Match.InvalidUserId";
        public const string FALLBACK_INVALID_USER_ID = "Invalid user id.";

        public const string CODE_MATCH_NOT_FOUND = "MATCH_NOT_FOUND";
        public const string MSG_MATCH_NOT_FOUND = "Match.NotFound";
        public const string FALLBACK_MATCH_NOT_FOUND = "Match not found.";

        public const string CODE_MATCH_NOT_IN_LOBBY = "MATCH_NOT_IN_LOBBY";
        public const string MSG_MATCH_NOT_IN_LOBBY = "Match.NotInLobby";
        public const string FALLBACK_MATCH_NOT_IN_LOBBY = "Match is not in lobby state.";

        public const string CODE_MATCH_NOT_IN_PROGRESS = "MATCH_NOT_IN_PROGRESS";
        public const string MSG_MATCH_NOT_IN_PROGRESS = "Match.NotInProgress";
        public const string FALLBACK_MATCH_NOT_IN_PROGRESS = "Match is not in progress.";

        public const string CODE_NOT_ENOUGH_PLAYERS = "MATCH_NOT_ENOUGH_PLAYERS";
        public const string MSG_NOT_ENOUGH_PLAYERS = "Match.NotEnoughPlayers";
        public const string FALLBACK_NOT_ENOUGH_PLAYERS = "Not enough players to start the match.";

        public const string CODE_PLAYERS_NOT_READY = "MATCH_PLAYERS_NOT_READY";
        public const string MSG_PLAYERS_NOT_READY = "Match.PlayersNotReady";
        public const string FALLBACK_PLAYERS_NOT_READY = "All players must be ready to start the match.";

        public const string CODE_HOST_NOT_AUTHORIZED = "MATCH_HOST_NOT_AUTHORIZED";
        public const string MSG_HOST_NOT_AUTHORIZED = "Match.HostNotAuthorized";
        public const string FALLBACK_HOST_NOT_AUTHORIZED = "Only the host can start this match.";

        public const string CODE_WINNER_NOT_IN_MATCH = "MATCH_WINNER_NOT_IN_MATCH";
        public const string MSG_WINNER_NOT_IN_MATCH = "Match.WinnerNotInMatch";
        public const string FALLBACK_WINNER_NOT_IN_MATCH = "Winner does not belong to this match.";

        public const string CODE_OPERATION_CONFLICT = "MATCH_OPERATION_CONFLICT";
        public const string MSG_OPERATION_CONFLICT = "Match.OperationConflict";
        public const string FALLBACK_OPERATION_CONFLICT = "The match changed while the request was being processed. Please retry.";
    }
}
