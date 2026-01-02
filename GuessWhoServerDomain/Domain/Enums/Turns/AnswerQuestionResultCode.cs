namespace GuessWhoServerDomain.Domain.Enums.Turns
{
    public enum AnswerQuestionResultCode
    {
        Success = 0,
        InvalidArgs = 1,
        MatchNotFound = 2,
        InvalidPhase = 3,
        NotOpponent = 4,
        PlayerNotActive = 5,
        TurnAdvanceConflict = 6,
        OperationConflict = 99
    }
}
