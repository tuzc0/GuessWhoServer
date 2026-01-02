namespace GuessWhoServerDomain.Domain.Enums.Turns
{
    public enum AskQuestionResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotFound = 2,
        NotYourTurn = 3,
        InvalidPhase = 4,
        PlayerNotActive = 5,
        OperationConflict = 99
    }
}
