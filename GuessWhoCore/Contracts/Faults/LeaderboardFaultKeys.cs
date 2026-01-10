namespace GuessWhoCore.Contracts.Faults
{
    public static class LeaderboardFaultKeys
    {
        public const string CODE_REQUEST_NULL = "LEADERBOARD_REQUEST_NULL";
        public const string MSG_REQUEST_NULL = "Leaderboard.RequestNull";
        public const string FALLBACK_REQUEST_NULL = "Leaderboard request cannot be null.";

        public const string CODE_INVALID_TOP_N = "LEADERBOARD_INVALID_TOPN";
        public const string MSG_INVALID_TOP_N = "Leaderboard.InvalidTopN";
        public const string FALLBACK_INVALID_TOP_N = "TopN must be a non-negative number.";

        public const string CODE_USER_NOT_FOUND = "LEADERBOARD_USER_NOT_FOUND";
        public const string MSG_USER_NOT_FOUND = "Leaderboard.UserNotFound";
        public const string FALLBACK_USER_NOT_FOUND = "User not found or inactive.";

        public const string CODE_UNEXPECTED_ERROR = "LEADERBOARD_UNEXPECTED_ERROR";
        public const string MSG_UNEXPECTED_ERROR = "Leaderboard.UnexpectedError";
        public const string FALLBACK_UNEXPECTED_ERROR = "An unexpected error occurred while retrieving the leaderboard.";
    }
}