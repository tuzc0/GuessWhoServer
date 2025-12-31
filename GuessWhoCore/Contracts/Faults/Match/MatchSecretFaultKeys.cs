
namespace GuessWhoCore.Contracts.Faults.Match
{
    public static class MatchSecretFaultKeys
    {
        public const string CODE_INVALID_REQUEST = "MATCH_SECRET_INVALID_REQUEST";
        public const string MSG_INVALID_REQUEST = "Match.Secret.InvalidRequest";
        public const string FALLBACK_INVALID_REQUEST = "Invalid request.";

        public const string CODE_INVALID_MATCH_ID = "MATCH_SECRET_INVALID_MATCH_ID";
        public const string MSG_INVALID_MATCH_ID = "Match.Secret.InvalidMatchId";
        public const string FALLBACK_INVALID_MATCH_ID = "Invalid match id.";

        public const string CODE_INVALID_USER_ID = "MATCH_SECRET_INVALID_USER_ID";
        public const string MSG_INVALID_USER_ID = "Match.Secret.InvalidUserId";
        public const string FALLBACK_INVALID_USER_ID = "Invalid user id.";

        public const string CODE_INVALID_CHARACTER = "MATCH_SECRET_INVALID_CHARACTER";
        public const string MSG_INVALID_CHARACTER = "Match.Secret.InvalidCharacter";
        public const string FALLBACK_INVALID_CHARACTER = "Selected character is not valid for this match.";

        public const string CODE_MATCH_NOT_FOUND = "MATCH_SECRET_MATCH_NOT_FOUND";
        public const string MSG_MATCH_NOT_FOUND = "Match.Secret.MatchNotFound";
        public const string FALLBACK_MATCH_NOT_FOUND = "Match not found.";

        public const string CODE_MATCH_NOT_IN_PROGRESS = "MATCH_SECRET_MATCH_NOT_IN_PROGRESS";
        public const string MSG_MATCH_NOT_IN_PROGRESS = "Match.Secret.MatchNotInProgress";
        public const string FALLBACK_MATCH_NOT_IN_PROGRESS = "Match is not in progress.";

        public const string CODE_PLAYER_NOT_IN_MATCH = "MATCH_SECRET_PLAYER_NOT_IN_MATCH";
        public const string MSG_PLAYER_NOT_IN_MATCH = "Match.Secret.PlayerNotInMatch";
        public const string FALLBACK_PLAYER_NOT_IN_MATCH = "Player does not belong to this match.";

        public const string CODE_PLAYER_ALREADY_LEFT = "MATCH_SECRET_PLAYER_ALREADY_LEFT";
        public const string MSG_PLAYER_ALREADY_LEFT = "Match.Secret.PlayerAlreadyLeft";
        public const string FALLBACK_PLAYER_ALREADY_LEFT = "Player already left the match.";

        public const string CODE_SECRET_ALREADY_CHOSEN = "MATCH_SECRET_ALREADY_CHOSEN";
        public const string MSG_SECRET_ALREADY_CHOSEN = "Match.Secret.AlreadyChosen";
        public const string FALLBACK_SECRET_ALREADY_CHOSEN = "Secret character was already chosen.";

        public const string CODE_OPERATION_CONFLICT = "MATCH_SECRET_OPERATION_CONFLICT";
        public const string MSG_OPERATION_CONFLICT = "Match.Secret.OperationConflict";
        public const string FALLBACK_OPERATION_CONFLICT = "The match changed while the request was being processed. Please retry.";
    }
}
