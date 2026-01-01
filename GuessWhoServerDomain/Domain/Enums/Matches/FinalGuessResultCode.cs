namespace GuessWhoServerDomain.Domain.Enums.Match
{
    public enum FinalGuessResultCode
    {
        Success = 0,
        MatchNotFound = 1,
        MatchNotInProgress = 2,
        NotYourTurn = 3,
        OpponentNotInMatch = 4,
        OpponentSecretNotSelected = 5,
        InvalidUser = 6,
        InvalidGuess = 7,
        EndMatchFailed = 8
    }
}
