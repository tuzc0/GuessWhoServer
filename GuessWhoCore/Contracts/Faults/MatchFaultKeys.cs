namespace GuessWhoCore.Contracts.Faults
{
    public static class MatchFaultKeys
    {
        public const string CODE_MATCH_NOT_FOUND = "MATCH_NOT_FOUND";
        public const string MSG_MATCH_NOT_FOUND = "Match.NotFound";
        public const string FALLBACK_MATCH_NOT_FOUND = "The match does not exist";

        public const string CODE_MATCH_NOT_JOINABLE = "MATCH_NOT_JOINABLE";
        public const string MSG_MATCH_NOT_JOINABLE = "Match.NotJoinable";
        public const string FALLBACK_MATCH_NOT_JOINABLE = "This match is not joinable";

        public const string CODE_MATCH_FULL = "MATCH_FULL";
        public const string MSG_MATCH_FULL = "Match.Full";
        public const string FALLBACK_MATCH_FULL = "The match is full";

        public const string CODE_IN_OTHER_ACTIVE_MATCH = "MATCH_IN_OTHER_ACTIVE_MATCH";
        public const string MSG_IN_OTHER_ACTIVE_MATCH = "Match.IsOtherActiveMatch";
        public const string FALLBACK_IN_OTHER_ACTIVE_MATCH = "You are already in another active match";

        public const string CODE_PLAYER_ALREADY_IN_MATCH = "MATCH_PLAYER_ALREADY_IN_MATCH";
        public const string MSG_PLAYER_ALREADY_IN_MATCH = "Match.PlayerAlreadyInMatch";
        public const string FALLBACK_PLAYER_ALREADY_IN_MATCH = "Player is already in this match";

        public const string CODE_PLAYER_NOT_IN_MATCH = "MATCH_PLAYER_NOT_IN_MATCH";
        public const string MSG_PLAYER_NOT_IN_MATCH = "Match.PlayerNotInMatch";
        public const string FALLBACK_PLAYER_NOT_IN_MATCH = "Player does not belong to this match";

        public const string CODE_PLAYER_ALREADY_LEFT = "MATCH_PLAYER_ALREADY_LEFT";
        public const string MSG_PLAYER_ALREADY_LEFT = "Match.PlayerAlreadyLeft";
        public const string FALLBACK_PLAYER_ALREADY_LEFT = "Player already left the match";

        public const string CODE_MATCH_NOT_IN_LOBBY = "MATCH_NOT_IN_LOBBY";
        public const string MSG_MATCH_NOT_IN_LOBBY = "Match.NotInLobby";
        public const string FALLBACK_MATCH_NOT_IN_LOBBY = "The match is not in lobby";

        public const string CODE_MATCH_NOT_IN_PROGRESS = "MATCH_NOT_IN_PROGRESS";
        public const string MSG_MATCH_NOT_IN_PROGRESS = "Match.NotInProgress";
        public const string FALLBACK_MATCH_NOT_IN_PROGRESS = "The match is not in progress";

        public const string CODE_PLAYERS_NOT_READY = "MATCH_PLAYERS_NOT_READY";
        public const string MSG_PLAYERS_NOT_READY = "Match.PlayersNotReady";
        public const string FALLBACK_PLAYERS_NOT_READY = "Players not ready";

        public const string CODE_NOT_ENOUGH_PLAYERS = "MATCH_NOT_ENOUGH_PLAYERS";
        public const string MSG_NOT_ENOUGH_PLAYERS = "Match.NotEnoughPlayers";
        public const string FALLBACK_NOT_ENOUGH_PLAYERS = "Not enough players to start.";

        public const string CODE_HOST_NOT_AUTHORIZED = "MATCH_HOST_NOT_AUTHORIZED";
        public const string MSG_HOST_NOT_AUTHORIZED = "Match.HostNotAuthorized";
        public const string FALLBACK_HOST_NOT_AUTHORIZED = "Host is not authorized.";

        public const string CODE_INVALID_CHARACTER = "MATCH_INVALID_CHARACTER";
        public const string MSG_INVALID_CHARACTER = "Match.InvalidCharacter";
        public const string FALLBACK_INVALID_CHARACTER = "Invalid character.";

        public const string CODE_SECRET_ALREADY_CHOSEN = "MATCH_SECRET_ALREADY_CHOSEN";
        public const string MSG_SECRET_ALREADY_CHOSEN = "Match.SecretAlreadyChosen";
        public const string FALLBACK_SECRET_ALREADY_CHOSEN = "Secret character already chosen.";

        public const string CODE_INSUFFICIENT_CHARACTERS = "MATCH_INSUFFICIENT_CHARACTERS";
        public const string MSG_INSUFFICIENT_CHARACTERS = "Match_InsufficientCharacters";
        public const string FALLBACK_INSUFFICIENT_CHARACTERS = 
            "There are not enough active characters available to start this game mode.";

        public const string CODE_DECK_GENERATION_FAILED = "MATCH_DECK_GENERATION_FAILED";
        public const string MSG_DECK_GENERATION_FAILED = "Match_DeckGenerationFailed";
        public const string FALLBACK_DECK_GENERATION_FAILED = 
            "An unexpected error occurred while attempting to generate the game board.";
    }
}
