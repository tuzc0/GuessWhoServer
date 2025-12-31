namespace GuessWhoCore.Contracts.Faults.Match
{
    public static class MatchDeckFaultKeys
    {
        public const string CODE_INVALID_REQUEST = "MATCH_DECK_INVALID_REQUEST";
        public const string MSG_INVALID_REQUEST = "MatchDeck.InvalidRequest";
        public const string FALLBACK_INVALID_REQUEST =
            "The request to obtain the match deck is invalid.";

        public const string CODE_INVALID_MATCH_ID = "MATCH_DECK_INVALID_MATCH_ID";
        public const string MSG_INVALID_MATCH_ID = "MatchDeck.InvalidMatchId";
        public const string FALLBACK_INVALID_MATCH_ID =
            "The match identifier is invalid.";

        public const string CODE_INSUFFICIENT_CHARACTERS = "MATCH_DECK_INSUFFICIENT_CHARACTERS";
        public const string MSG_INSUFFICIENT_CHARACTERS = "MatchDeck.InsufficientCharacters";
        public const string FALLBACK_INSUFFICIENT_CHARACTERS =
            "There are not enough active characters to generate the match deck. Please try again later.";

        public const string CODE_DECK_GENERATION_FAILED = "MATCH_DECK_GENERATION_FAILED";
        public const string MSG_DECK_GENERATION_FAILED = "MatchDeck.GenerationFailed";
        public const string FALLBACK_DECK_GENERATION_FAILED =
            "The match deck could not be generated. Please try again.";

        public const string CODE_OPERATION_CONFLICT = "MATCH_DECK_OPERATION_CONFLICT";
        public const string MSG_OPERATION_CONFLICT = "MatchDeck.OperationConflict";
        public const string FALLBACK_OPERATION_CONFLICT =
            "The match was updated by another request at the same time. Please try again.";
    }
}
