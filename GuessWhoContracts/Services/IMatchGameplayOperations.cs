using GuessWhoCore.Contracts.Faults;
using GuessWhoCore.Contracts.Requests;
using GuessWhoCore.Contracts.Response;
using System.ServiceModel;

namespace GuessWhoContracts.Services
{
    [ServiceContract]
    public interface IMatchGameplayOperations
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse StartMatch(StartMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse EndMatch(EndMatchRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse ChooseSecretCharacter(ChooseSecretCharacterRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        MatchDeckResponse GetMatchDeck(GetOrCreateMatchDeckRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse AskQuestion(AskQuestionRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        BasicResponse AnswerQuestion(AnswerQuestionRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        PassTurnResponse PassTurn(PassTurnRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        ClaimTimeoutResponse ClaimTimeout(ClaimTimeoutRequest request);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        FinalGuessResponse FinalGuess(FinalGuessRequest request);
    }
}
