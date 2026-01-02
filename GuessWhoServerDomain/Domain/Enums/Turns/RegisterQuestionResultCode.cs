namespace GuessWhoServerDomain.Domain.Enums.Turns
{
    public enum RegisterQuestionResultCode
    {
        Success = 0,
        OperationConflict = 99
    }

    public enum RegisterAnswerResultCode
    {
        Success = 0,
        OperationConflict = 99
    }

    public enum SetTurnPhaseResultCode
    {
        Success = 0,
        PhaseMismatch = 1,
        OperationConflict = 99
    }
}
