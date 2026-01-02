using GuessWhoServerDomain.Domain.Models.Turns;


namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchQuestionRepository
    {
        TurnPhaseSnapshot GetTurnPhase(long matchId);

        RegisterQuestionResult RegisterQuestion(RegisterQuestionArgs questionArgs);

        // ✅ Cambio: answerOptionId (int) en args
        RegisterAnswerResult RegisterAnswer(RegisterAnswerArgs answerArgs);

        SetTurnPhaseResult SetTurnPhase(SetTurnPhaseArgs phaseArgs);
    }
}
