using GuessWhoServerDomain.Domain.Models.Turns;
using GuessWhoServerDomain.Domain.Parameters.Turns;
using GuessWhoServerDomain.Domain.Results.Turns;


namespace GuessWhoServerDomain.Domain.Interfaces.Repositories
{
    public interface IMatchQuestionRepository
    {
        TurnPhaseSnapshot GetTurnPhase(long matchId);

        RegisterQuestionResult RegisterQuestion(RegisterQuestionArgs questionArgs);

        RegisterAnswerResult RegisterAnswer(RegisterAnswerArgs answerArgs);

        SetTurnPhaseResult SetTurnPhase(SetTurnPhaseArgs phaseArgs);
    }
}
