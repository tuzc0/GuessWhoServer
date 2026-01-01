namespace GuessWhoCore.Contracts.Faults.Match
{
    public static class MatchLifecycleFaultKeys
    {
        public const string CODE_INVALID_PROFILE_ID = "MATCH_INVALID_PROFILE_ID";
        public const string MSG_INVALID_PROFILE_ID = "Match.InvalidProfileId";
        public const string FALLBACK_INVALID_PROFILE_ID = "Profile id is invalid.";

        public const string CODE_MATCH_ALREADY_PRIVATE = "MATCH_ALREADY_PRIVATE";
        public const string MSG_MATCH_ALREADY_PRIVATE = "Match.AlreadyPrivate";
        public const string FALLBACK_MATCH_ALREADY_PRIVATE = "The match is already private.";

        public const string CODE_INVALID_REQUEST = "MATCH_INVALID_REQUEST";
        public const string MSG_INVALID_REQUEST = "Match.InvalidRequest";
        public const string FALLBACK_INVALID_REQUEST = "The request is invalid.";

        public const string CODE_INVALID_MATCH_ID = "MATCH_INVALID_MATCH_ID";
        public const string MSG_INVALID_MATCH_ID = "Match.InvalidMatchId";
        public const string FALLBACK_INVALID_MATCH_ID = "Match id is invalid.";

        public const string CODE_INVALID_USER_ID = "MATCH_INVALID_USER_ID";
        public const string MSG_INVALID_USER_ID = "Match.InvalidUserId";
        public const string FALLBACK_INVALID_USER_ID = "User id is invalid.";

        public const string CODE_INVALID_WINNER = "MATCH_INVALID_WINNER";
        public const string MSG_INVALID_WINNER = "Match.InvalidWinner";
        public const string FALLBACK_INVALID_WINNER = "Winner user id is invalid.";

        public const string CODE_HOST_NOT_AUTHORIZED = "MATCH_HOST_NOT_AUTHORIZED";
        public const string MSG_HOST_NOT_AUTHORIZED = "Match.HostNotAuthorized";
        public const string FALLBACK_HOST_NOT_AUTHORIZED = "Only the host can perform this action.";

        public const string CODE_MATCH_NOT_FOUND = "MATCH_NOT_FOUND";
        public const string MSG_MATCH_NOT_FOUND = "Match.NotFound";
        public const string FALLBACK_MATCH_NOT_FOUND = "The match was not found.";

        public const string CODE_MATCH_NOT_IN_LOBBY = "MATCH_NOT_IN_LOBBY";
        public const string MSG_MATCH_NOT_IN_LOBBY = "Match.NotInLobby";
        public const string FALLBACK_MATCH_NOT_IN_LOBBY = "The match is not in lobby state.";

        public const string CODE_NOT_ENOUGH_PLAYERS = "MATCH_NOT_ENOUGH_PLAYERS";
        public const string MSG_NOT_ENOUGH_PLAYERS = "Match.NotEnoughPlayers";
        public const string FALLBACK_NOT_ENOUGH_PLAYERS = "Not enough players to start the match.";

        public const string CODE_PLAYERS_NOT_READY = "MATCH_PLAYERS_NOT_READY";
        public const string MSG_PLAYERS_NOT_READY = "Match.PlayersNotReady";
        public const string FALLBACK_PLAYERS_NOT_READY = "All players must be ready to start the match.";

        public const string CODE_MATCH_NOT_IN_PROGRESS = "MATCH_NOT_IN_PROGRESS";
        public const string MSG_MATCH_NOT_IN_PROGRESS = "Match.NotInProgress";
        public const string FALLBACK_MATCH_NOT_IN_PROGRESS = "The match is not in progress.";

        public const string CODE_WINNER_NOT_IN_MATCH = "MATCH_WINNER_NOT_IN_MATCH";
        public const string MSG_WINNER_NOT_IN_MATCH = "Match.WinnerNotInMatch";
        public const string FALLBACK_WINNER_NOT_IN_MATCH = "The winner does not belong to this match.";

        public const string CODE_OPERATION_CONFLICT = "MATCH_OPERATION_CONFLICT";
        public const string MSG_OPERATION_CONFLICT = "Match.OperationConflict";
        public const string FALLBACK_OPERATION_CONFLICT = "The operation could not be completed due to a concurrent update.";

        public const string CODE_INVALID_MATCH_CODE = "MATCH_INVALID_MATCH_CODE";
        public const string MSG_INVALID_MATCH_CODE = "Match.InvalidMatchCode";
        public const string FALLBACK_INVALID_MATCH_CODE = "Match code is invalid.";

        public const string CODE_MATCH_NOT_PUBLIC = "MATCH_NOT_PUBLIC";
        public const string MSG_MATCH_NOT_PUBLIC = "Match.NotPublic";
        public const string FALLBACK_MATCH_NOT_PUBLIC = "The match is not public.";

        public const string CODE_PLAYER_NOT_IN_MATCH = "MATCH_PLAYER_NOT_IN_MATCH";
        public const string MSG_PLAYER_NOT_IN_MATCH = "Match.PlayerNotInMatch";
        public const string FALLBACK_PLAYER_NOT_IN_MATCH = "Player does not belong to this match.";

        public const string CODE_PLAYER_ALREADY_LEFT = "MATCH_PLAYER_ALREADY_LEFT";
        public const string MSG_PLAYER_ALREADY_LEFT = "Match.PlayerAlreadyLeft";
        public const string FALLBACK_PLAYER_ALREADY_LEFT = "Player already left the match.";

        public const string CODE_INVALID_CHARACTER = "MATCH_INVALID_CHARACTER";
        public const string MSG_INVALID_CHARACTER = "Match.InvalidCharacter";
        public const string FALLBACK_INVALID_CHARACTER = "Selected character is not valid for this match.";

        public const string CODE_SECRET_ALREADY_CHOSEN = "MATCH_SECRET_ALREADY_CHOSEN";
        public const string MSG_SECRET_ALREADY_CHOSEN = "Match.SecretAlreadyChosen";
        public const string FALLBACK_SECRET_ALREADY_CHOSEN = "Secret character was already chosen.";
    }
}
